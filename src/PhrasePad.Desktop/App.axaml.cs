using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PhrasePad.Core.Expansion;
using PhrasePad.Desktop.ViewModels;
using PhrasePad.Desktop.Views;

namespace PhrasePad.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var expansionEngine = new ExpansionEngine(new ITokenResolver[]
            {
                new DateTimeTokenResolver(TimeProvider.System)
            });

            desktop.MainWindow = new MainWindow
            {
                DataContext = new SnippetEditorViewModel(expansionEngine),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
