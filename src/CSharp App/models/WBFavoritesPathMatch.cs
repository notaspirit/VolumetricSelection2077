using VolumetricSelection2077.Models.WorldBuilder.Favorites;

namespace VolumetricSelection2077.Models;

public class WBFavoritesPathMatch
{
    public string FilePath { get; set; }
    public int Index { get; set; }
    public FavoritesRoot FavRoot { get; set; }

    public WBFavoritesPathMatch()
    {
        FavRoot = new FavoritesRoot();
        FilePath = "";
        Index = 0;
    }
}