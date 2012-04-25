﻿using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using RT.Util;

namespace TankIconMaker.Layers
{
    public enum ImageStyle { Contour, [Description("3D")] ThreeD }

    sealed class TankImageLayer : LayerBaseWpf
    {
        public override int Version { get { return 1; } }
        public override string TypeName { get { return "Image / Tank"; } }
        public override string TypeDescription { get { return "Draws a tank in one of several styles."; } }

        [Category("Image")]
        [Description("Chooses one of the standard tank image styles.")]
        public ImageStyle Style { get; set; }

        public override void Draw(Tank tank, DrawingContext dc)
        {
            var image = Style == ImageStyle.Contour ? tank.GetImageContour() : tank.GetImage3D();
            if (image == null)
            {
                tank.AddWarning("The image for this tank is missing.");
                return;
            }
            dc.DrawImage(image, new Rect(0, 0, image.Width / image.Height * 24.0, 24));
        }
    }

    sealed class CurrentImageLayer : LayerBaseWpf
    {
        public override int Version { get { return 1; } }
        public override string TypeName { get { return "Image / Current"; } }
        public override string TypeDescription { get { return "Draws the icon that’s currently saved in the output directory."; } }

        public override void Draw(Tank tank, DrawingContext dc)
        {
            var image = tank.GetImageCurrent();
            if (image == null)
            {
                tank.AddWarning("There is no current image for this tank.");
                return;
            }
            dc.DrawImage(image);
        }
    }

    sealed class CustomImageLayer : LayerBaseWpf
    {
        public override int Version { get { return 1; } }
        public override string TypeName { get { return "Image / Custom"; } }
        public override string TypeDescription { get { return "Draws an arbitrary, user-supplied image."; } }

        public ValueSelector<Filename> ImageFile { get; set; }

        public CustomImageLayer()
        {
            ImageFile = new ValueSelector<Filename>("");
        }

        public override void Draw(Tank tank, DrawingContext dc)
        {
            var filename = ImageFile.GetValue(tank);
            if (string.IsNullOrWhiteSpace(filename))
                return;

            var image = ImageCache.GetImage(PathUtil.AppPathCombine(filename));
            if (image == null && Program.LastGameInstallSettings != null)
            {
                image = ImageCache.GetImage(Path.Combine(Program.LastGameInstallSettings.Path, Program.LastGameInstallSettings.GameVersion.PathMods, filename));
                if (image == null)
                    image = ImageCache.GetImage(Path.Combine(Program.LastGameInstallSettings.Path, filename));
            }
            if (image == null)
            {
                tank.AddWarning("The image {0} could not be found.".Fmt(filename));
                return;
            }
            image.Freeze();
            dc.DrawImage(image);
        }
    }
}
