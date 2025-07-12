namespace VolumetricSelection2077.Models;

public class WindowRecoveryState
{
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int PosWidth { get; set; }
    public int PosHeight { get; set; }
    public int WindowState { get; set; }

    public WindowRecoveryState()
    {
        PosX = 250;
        PosY = 250;
        PosWidth = 1300;
        PosHeight = 600;
        WindowState = 0;
    }
}