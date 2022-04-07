using UnityEngine;
using System.IO;

public class SaveImages
{
    public static void TextureFromColourMap(Color[] colourMap, int width, int height, int seed) {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels (colourMap);
        texture.Apply ();

        SaveTextureAsPNG(texture, "PopulationDensity_" + seed);
    }

    public static void SaveTextureAsPNG(Texture2D texture, string fileName) {
        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/../SaveImages/";
        if(!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + fileName + ".png", bytes);
    }

    public static void SaveMatrixAsPNG(float[,] matrix, int seed) {
        int width = matrix.GetLength (0);
        int height = matrix.GetLength (1);
        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colourMap [y * width + x] = Color.Lerp (Color.black, Color.white, matrix [x, y]);
            }
        }
        TextureFromColourMap (colourMap, width, height, seed);
    }
}