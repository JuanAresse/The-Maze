using UnityEngine;

public static class GameOptions
{
    public static readonly int[] Sizes = { 4, 6, 8 };
    public static int MazeSize { get; private set; } = 6;
    public static bool SelectionReady { get; private set; }

    public static void ChooseSize(int size)
    {
        MazeSize = System.Array.IndexOf(Sizes, size) >= 0 ? size : 6;
        SelectionReady = true;
        PlayerPrefs.SetInt("MazeSize", MazeSize);
    }

    public static int SavedSize => PlayerPrefs.GetInt("MazeSize", 6);
}
