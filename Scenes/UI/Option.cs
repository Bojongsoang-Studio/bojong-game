namespace BojongGame.Scenes.UI;

public class Option
{
    public static bool AlternativeKeybind { get; private set; }

    public static void ToggleAlternativeKeybind()
    {
        AlternativeKeybind = !AlternativeKeybind;
    }
}