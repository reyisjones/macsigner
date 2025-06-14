using Avalonia.Controls;
using System;

namespace MacSigner;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Console.WriteLine("MainWindow constructor called");
        InitializeComponent();
        Console.WriteLine("MainWindow InitializeComponent completed");
        
        // Ensure window is visible
        this.WindowState = WindowState.Normal;
        this.Topmost = false;
        
        Console.WriteLine($"MainWindow initialized - Width: {Width}, Height: {Height}");
    }
}