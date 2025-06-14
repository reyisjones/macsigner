using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MacSigner.Models;
using MacSigner.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MacSigner.Commands
{
    public class SignCommand : Command
    {
        public SignCommand() : base("sign", "Sign executable files using Azure Trusted Signing")
        {
            var pathOption = new Option<string>(
                aliases: new[] { "--path", "-p" },
                description: "Path to the directory or file to sign")
            {
                IsRequired = true
            };

            var tenantIdOption = new Option<string>(
                aliases: new[] { "--tenant-id", "-t" },
                description: "Azure tenant ID");

            var clientIdOption = new Option<string>(
                aliases: new[] { "--client-id", "-c" },
                description: "Azure client ID");

            var clientSecretOption = new Option<string>(
                aliases: new[] { "--client-secret", "-s" },
                description: "Azure client secret");

            var endpointOption = new Option<string>(
                aliases: new[] { "--endpoint", "-e" },
                description: "Trusted Signing endpoint URL");

            var profileOption = new Option<string>(
                aliases: new[] { "--profile", "-r" },
                description: "Certificate profile name");

            var recursiveOption = new Option<bool>(
                aliases: new[] { "--recursive", "-R" },
                description: "Scan directories recursively",
                getDefaultValue: () => true);

            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: "Enable verbose output",
                getDefaultValue: () => false);

            AddOption(pathOption);
            AddOption(tenantIdOption);
            AddOption(clientIdOption);
            AddOption(clientSecretOption);
            AddOption(endpointOption);
            AddOption(profileOption);
            AddOption(recursiveOption);
            AddOption(verboseOption);

            this.SetHandler(async (InvocationContext context) =>
            {
                var path = context.ParseResult.GetValueForOption(pathOption)!;
                var tenantId = context.ParseResult.GetValueForOption(tenantIdOption);
                var clientId = context.ParseResult.GetValueForOption(clientIdOption);
                var clientSecret = context.ParseResult.GetValueForOption(clientSecretOption);
                var endpoint = context.ParseResult.GetValueForOption(endpointOption);
                var profile = context.ParseResult.GetValueForOption(profileOption);
                var recursive = context.ParseResult.GetValueForOption(recursiveOption);
                var verbose = context.ParseResult.GetValueForOption(verboseOption);

                await ExecuteSignCommand(path, tenantId, clientId, clientSecret, endpoint, profile, recursive, verbose);
            });
        }

        private static async Task ExecuteSignCommand(
            string path,
            string? tenantId,
            string? clientId,
            string? clientSecret,
            string? endpoint,
            string? profile,
            bool recursive,
            bool verbose)
        {
            // Configure services
            var services = new ServiceCollection();
            
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
            });

            services.AddHttpClient<ITrustedSigningService, AzureTrustedSigningService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ITrustedSigningService, AzureTrustedSigningService>();
            services.AddSingleton<ISettingsService, SettingsService>();

            // Load settings
            services.AddSingleton(provider =>
            {
                var settingsService = provider.GetRequiredService<ISettingsService>();
                var settings = settingsService.LoadSettingsAsync().Result;

                // Override with command line options if provided
                if (!string.IsNullOrEmpty(tenantId)) settings.AzureTenantId = tenantId;
                if (!string.IsNullOrEmpty(clientId)) settings.AzureClientId = clientId;
                if (!string.IsNullOrEmpty(clientSecret)) settings.AzureClientSecret = clientSecret;
                if (!string.IsNullOrEmpty(endpoint)) settings.TrustedSigningEndpoint = endpoint;
                if (!string.IsNullOrEmpty(profile)) settings.CertificateProfileName = profile;

                return settings;
            });

            using var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<SignCommand>>();
            var fileService = serviceProvider.GetRequiredService<IFileService>();
            var signingService = serviceProvider.GetRequiredService<ITrustedSigningService>();
            var settings = serviceProvider.GetRequiredService<AppSettings>();

            try
            {
                Console.WriteLine("MacSigner CLI - Digital Code Signing Tool");
                Console.WriteLine("=========================================");

                // Validate settings
                if (!settings.IsConfigured())
                {
                    Console.WriteLine("❌ Error: Azure Trusted Signing is not properly configured.");
                    Console.WriteLine("Please provide all required parameters or configure them in settings:");
                    Console.WriteLine("  --tenant-id     Azure Tenant ID");
                    Console.WriteLine("  --client-id     Azure Client ID");
                    Console.WriteLine("  --client-secret Azure Client Secret");
                    Console.WriteLine("  --endpoint      Trusted Signing Endpoint");
                    Console.WriteLine("  --profile       Certificate Profile Name");
                    Environment.Exit(1);
                    return;
                }

                // Validate path
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    Console.WriteLine($"❌ Error: Path does not exist: {path}");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine($"📁 Scanning path: {path}");
                Console.WriteLine($"🔄 Recursive scan: {recursive}");

                // Scan for files
                var signableFiles = new List<SignableFile>();

                if (File.Exists(path))
                {
                    // Single file
                    var file = new SignableFile(path);
                    if (file.IsSignableFileType())
                    {
                        signableFiles.Add(file);
                    }
                    else
                    {
                        Console.WriteLine($"⚠️  Warning: File type not supported for signing: {path}");
                        Environment.Exit(1);
                        return;
                    }
                }
                else
                {
                    // Directory
                    signableFiles = await fileService.ScanDirectoryAsync(path, recursive);
                }

                if (!signableFiles.Any())
                {
                    Console.WriteLine("❌ No signable files found.");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine($"✅ Found {signableFiles.Count} signable files:");
                foreach (var file in signableFiles)
                {
                    Console.WriteLine($"   • {file.FileName} ({file.GetFormattedFileSize()})");
                }

                Console.WriteLine();
                Console.WriteLine("🔐 Authenticating with Azure Trusted Signing...");

                // Authenticate
                var authenticated = await signingService.AuthenticateAsync();
                if (!authenticated)
                {
                    Console.WriteLine("❌ Failed to authenticate with Azure Trusted Signing service.");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine("✅ Authentication successful.");

                // Create signing request
                var signingRequest = new SigningRequest();
                signingRequest.Files.AddRange(signableFiles);

                Console.WriteLine($"📝 Submitting signing request for {signableFiles.Count} files...");

                // Submit request
                var requestId = await signingService.SubmitSigningRequestAsync(signingRequest);
                Console.WriteLine($"✅ Signing request submitted. Request ID: {requestId}");

                // Monitor progress
                Console.WriteLine("⏳ Monitoring signing progress...");
                var startTime = DateTime.UtcNow;
                var timeout = TimeSpan.FromMinutes(10); // 10 minute timeout

                while (DateTime.UtcNow - startTime < timeout)
                {
                    var status = await signingService.GetSigningStatusAsync(requestId);
                    
                    Console.Write($"\r🔄 Status: {status}");

                    if (status == SigningStatus.Completed)
                    {
                        Console.WriteLine();
                        Console.WriteLine("✅ Signing completed successfully!");
                        
                        // TODO: Download signed files and replace originals
                        Console.WriteLine("📥 Downloading signed files...");
                        
                        var successCount = 0;
                        foreach (var file in signableFiles)
                        {
                            try
                            {
                                var signedFileData = await signingService.DownloadSignedFileAsync(requestId, file.FileName);
                                if (signedFileData != null)
                                {
                                    var success = await fileService.ReplaceFileWithSignedVersionAsync(file.FilePath, signedFileData);
                                    if (success)
                                    {
                                        successCount++;
                                        Console.WriteLine($"✅ {file.FileName} - Signed successfully");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"❌ {file.FileName} - Failed to replace with signed version");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"❌ {file.FileName} - Failed to download signed version");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ {file.FileName} - Error: {ex.Message}");
                            }
                        }

                        Console.WriteLine();
                        Console.WriteLine($"🎉 Signing complete! {successCount}/{signableFiles.Count} files signed successfully.");
                        Environment.Exit(successCount == signableFiles.Count ? 0 : 1);
                        return;
                    }
                    else if (status == SigningStatus.Failed)
                    {
                        Console.WriteLine();
                        Console.WriteLine("❌ Signing failed.");
                        Environment.Exit(1);
                        return;
                    }
                    else if (status == SigningStatus.Cancelled)
                    {
                        Console.WriteLine();
                        Console.WriteLine("⚠️  Signing was cancelled.");
                        Environment.Exit(1);
                        return;
                    }

                    await Task.Delay(5000); // Poll every 5 seconds
                }

                Console.WriteLine();
                Console.WriteLine("⏰ Timeout waiting for signing to complete.");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute sign command");
                Console.WriteLine($"❌ Error: {ex.Message}");
                if (verbose)
                {
                    Console.WriteLine(ex.StackTrace);
                }
                Environment.Exit(1);
            }
        }
    }
}
