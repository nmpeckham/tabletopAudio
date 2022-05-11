using UnityEngine;
using UnityEngine.UI;

public class ImageTest : MonoBehaviour
{
    private readonly Texture2D testTexture;
    public Image testImage;

    // Start is called before the first frame update
    private void Start()
    {
        Texture2D testing = new Texture2D(100, 100);
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                testing.SetPixel(i, j, Color.green);
            }

        }
        testing.Apply();
        Sprite test = Sprite.Create(testing, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f));
        testImage.sprite = test;
    }
}
