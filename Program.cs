using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Text;
using System.Threading.Tasks;

try
{

    Console.Write("Enter the path to the photos directory: ");
    string imageDirectory = Console.ReadLine() ?? string.Empty;

    Console.Write("Enter the path to the output directory: ");
    string outputDirectory = Console.ReadLine() ?? string.Empty;

    Console.Write("Enter the path to the text dictionary file: ");
    string textFilePath = Console.ReadLine() ?? string.Empty;

    if (!Directory.Exists(imageDirectory) || !Directory.Exists(outputDirectory) || !File.Exists(textFilePath))
    {
        Console.WriteLine("One or more paths are incorrect. Please check and try again.");
        return;
    }

    Console.WriteLine("Processing images...");

    var texts = new Dictionary<string, string>();
    string[] lines = File.ReadAllLines(textFilePath);
    foreach (string line in lines)
    {
        string[] parts = line.Split(new[] { ": " }, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            texts.Add(parts[0], parts[1]);
        }
    }

    // Define horizontal A4 size in pixels (assuming 300 DPI)
    Size a4Size = new Size(3508, 2480);

    Parallel.ForEach(texts, item =>
    {
        string imagePath = System.IO.Path.Combine(imageDirectory, item.Key);
        string text = item.Value;

        using (Image<Rgba32> image = Image.Load<Rgba32>(imagePath))
        {
            int dpi = 300;
            float baseFontSize = 25; // Base font size for 150 DPI
            float scaleFactor = dpi / 150f;
            float calculatedFontSize = baseFontSize * scaleFactor;

            var font = SystemFonts.CreateFont("Arial", calculatedFontSize);

            // Wrap the text and measure its size
            string wrappedText = WrapText(text, font, a4Size.Width - 50);
            FontRectangle wrappedTextSize = TextMeasurer.MeasureBounds(wrappedText, new RendererOptions(font));

            // Determine the available height for the image
            int availableImageHeight = a4Size.Height - (int)wrappedTextSize.Height - 75; // 75 for margins
            Size newSize = new Size(a4Size.Width, availableImageHeight);

            using (Image<Rgba32> a4Image = new Image<Rgba32>(Configuration.Default, a4Size.Width, a4Size.Height, Color.White))
            {
                a4Image.Mutate(ctx =>
                {
                    ctx.DrawImage(image.Clone(ic => ic.Resize(newSize)), new Point(0, 0), 1);

                    // Wrap the text manually
                    var wrappedTextLines = WrapText(text, font, a4Size.Width - 50).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                    // Draw each line, centering it horizontally
                    float textY = newSize.Height + 25;
                    foreach (var line in wrappedTextLines)
                    {
                        var textSize = TextMeasurer.MeasureSize(line, new RendererOptions(font));
                        float textX = (a4Size.Width - textSize.Width) / 2;

                        ctx.DrawText(line, font, Color.Black, new PointF(textX, textY));
                        textY += textSize.Height; // Move to the next line
                    }
                });

                string outputPath = System.IO.Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(item.Key) + ".jpg");

                var options = new JpegEncoder
                {
                    Quality = 85
                };

                a4Image.Save(outputPath, options);
            }
        }
    });

    Console.WriteLine("All images processed!");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

static string WrapText(string text, Font font, float maxWidth)
{
    var words = text.Split(' ');
    var wrappedText = new StringBuilder();
    var currentLine = new StringBuilder();

    foreach (var word in words)
    {
        var potentialLine = currentLine.Length > 0 ? currentLine.ToString() + " " + word : word;
        var potentialLineWidth = TextMeasurer.MeasureSize(potentialLine, new RendererOptions(font)).Width;

        if (potentialLineWidth < maxWidth)
        {
            currentLine.Append(currentLine.Length > 0 ? " " + word : word);
        }
        else
        {
            wrappedText.AppendLine(currentLine.ToString());
            currentLine.Clear().Append(word);
        }
    }

    wrappedText.Append(currentLine);
    return wrappedText.ToString();
}

internal class RendererOptions : TextOptions
{
    public RendererOptions(Font font) : base(font)
    {
    }
}