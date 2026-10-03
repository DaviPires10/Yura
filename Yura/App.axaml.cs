using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Sentry;
using System;
using System.IO;

namespace Yura
{
    public partial class App : Application
    {
        private const string SentryDsn = "https://2bca1d6075984d7fb74e569f4f6ff3a1@o4503985120215040.ingest.sentry.io/4503985128341504";

        private MainWindow? _window;

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
#if !DEBUG
            SentrySdk.Init(options =>
            {
                options.Dsn = SentryDsn;
                options.IsGlobalModeEnabled = true;
                options.SetBeforeSend(evt =>
                {
                    if (_window?.Bigfile != null)
                    {
                        evt.SetExtra("bigfile", _window.Bigfile.Name);
                        evt.SetExtra("game", _window.Game);
                    }
                    return evt;
                });
            });
#endif

            SetTheme((Theme)Settings.Default.Theme);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _window = new MainWindow();
                desktop.MainWindow = _window;
                desktop.Exit += OnExit;

                var args = desktop.Args ?? Array.Empty<string>();
                if (args.Length > 0 && File.Exists(args[^1]))
                {
                    var options = new CommandLineOptions(args);
                    if (options.HasOption("-game"))
                        _window.OpenBigfile(options.Bigfile, options);
                    else
                        _window.OpenBigfileDialog(options.Bigfile);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Switch the active theme. Avalonia re-evaluates every DynamicResource
        /// binding automatically, so this is instant and safe at any time.
        /// </summary>
        public void SetTheme(Theme theme)
        {
            RequestedThemeVariant = theme == Theme.Light ? ThemeVariant.Light : ThemeVariant.Dark;
        }

        private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            var folder = Path.Combine(Path.GetTempPath(), "Yura");
            if (Directory.Exists(folder))
            {
                try { Directory.Delete(folder, true); } catch { }
            }
        }
    }
}
