namespace Tasklet;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();

        MainPage = new NavigationPage(mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#006400"),
            BarTextColor = Colors.White
        };
    }
}