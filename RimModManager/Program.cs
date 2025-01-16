using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.UI;
using RimModManager;

AppBuilder.Create()
    .AddWindow<MainWindow>(show: true, mainWindow: true)
    .AddTitleBar<TitleBar>()
    .Run();