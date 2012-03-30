﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using RT.Util.Xml;
using D = System.Drawing;

namespace TankIconMaker
{
    class MakerDarkAgent : MakerBaseWpf
    {
        public override string Name { get { return "Dark Agent (Black Spy replica)"; } }
        public override string Author { get { return "Romkyns"; } }
        public override int Version { get { return 1; } }
        public override string Description { get { return "Recreates my favourite icons by Black_Spy in TankIconMaker. Kudos to Black_Spy for the design!"; } }

        // Category: Tank name
        [Category("Tank name"), DisplayName("Data source")]
        [Description("Choose the name of the property that supplies the data for the bottom right location.")]
        [Editor(typeof(DataSourceEditor), typeof(DataSourceEditor))]
        public ExtraPropertyId NameData { get; set; }

        [Category("Tank name"), DisplayName("Color: normal")]
        [Description("Used to color the name of all tanks that can be freely bought for silver in the game.")]
        public Color NameColorNormal { get; set; }

        [Category("Tank name"), DisplayName("Color: premium")]
        [Description("Used to color the name of all premium tanks, that is tanks that can be freely bought for gold in the game.")]
        public Color NameColorPremium { get; set; }

        [Category("Tank name"), DisplayName("Color: special")]
        [Description("Used to color the name of all special tanks, that is tanks that cannot normally be bought in the game.")]
        public Color NameColorSpecial { get; set; }

        [Category("Tank name"), DisplayName("Rendering style")]
        [Description("Determines how the tank name should be anti-aliased.")]
        public TextAntiAliasStyle NameAntiAlias { get; set; }

        // Category: Tank tier
        [Category("Tank tier"), DisplayName("Tier  1 Color")]
        [Description("The color of the tier text for tier 1 tanks. The color for tiers 2..9 is interpolated based on tier 1, 5 and 10 settings.")]
        public Color Tier1Color { get; set; }

        [Category("Tank tier"), DisplayName("Tier  5 Color")]
        [Description("The color of the tier text for tier 5 tanks. The color for tiers 2..9 is interpolated based on tier 1, 5 and 10 settings.")]
        public Color Tier5Color { get; set; }

        [Category("Tank tier"), DisplayName("Tier 10 Color")]
        [Description("The color of the tier text for tier 10 tanks. The color for tiers 2..9 is interpolated based on tier 1, 5 and 10 settings.")]
        public Color Tier10Color { get; set; }

        [Category("Tank tier"), DisplayName("Rendering style")]
        [Description("Determines how the tank name should be anti-aliased.")]
        public TextAntiAliasStyle TierAntiAlias { get; set; }

        // Category: Tank image
        [Category("Tank image"), DisplayName("Overhang")]
        [Description("Indicates whether the tank picture should overhang above and below the background rectangle, fit strictly inside it or be clipped to its size.")]
        public OverhangStyle Overhang { get; set; }
        public enum OverhangStyle { Overhang, [Description("Fit inside frame")] Fit, [Description("Clip to frame")] Clip }

        [Category("Tank image"), DisplayName("Style")]
        [Description("Specifies one of the built-in image styles to use.")]
        public ImageStyle Style { get; set; }
        public enum ImageStyle { Contour, [Description("3D")] ThreeD, None }

        [Category("Tank image"), DisplayName("Color: light tank")]
        [Description("Colorization to apply to the light tank images. Make sure to crank up Alpha to see the effect.")]
        public Color TankColorizeLight { get; set; }

        [Category("Tank image"), DisplayName("Color: medium tank")]
        [Description("Colorization to apply to the medium tank images. Make sure to crank up Alpha to see the effect.")]
        public Color TankColorizeMedium { get; set; }

        [Category("Tank image"), DisplayName("Color: heavy tank")]
        [Description("Colorization to apply to the heavy tank images. Make sure to crank up Alpha to see the effect.")]
        public Color TankColorizeHeavy { get; set; }

        [Category("Tank image"), DisplayName("Color: destroyer")]
        [Description("Colorization to apply to the destroyer images. Make sure to crank up Alpha to see the effect.")]
        public Color TankColorizeDestroyer { get; set; }

        [Category("Tank image"), DisplayName("Color: artillery")]
        [Description("Colorization to apply to the artillery images. Make sure to crank up Alpha to see the effect.")]
        public Color TankColorizeArtillery { get; set; }

        [Category("Tank image"), DisplayName("Opacity")]
        [Description("The opacity of the tank images.")]
        public int TankOpacity { get { return _TankOpacity; } set { _TankOpacity = Math.Max(0, Math.Min(255, value)); } }
        private int _TankOpacity;

        [Category("Tank image"), DisplayName("Alignment")]
        [Description("0 = center relative to text, 1 = center in rectangle, other positive values: left margin, other negative values: right margin.")]
        public int TankAlignment { get; set; }

        public MakerDarkAgent()
        {
            NameData = new ExtraPropertyId("NameShortWG", "Ru", "Romkyns");
            NameColorNormal = Color.FromRgb(210, 210, 210);
            NameColorPremium = Colors.Yellow;
            NameColorSpecial = Color.FromRgb(242, 98, 103);
            NameAntiAlias = TextAntiAliasStyle.Aliased;

            TierAntiAlias = TextAntiAliasStyle.Aliased;
            Tier1Color = NameColorNormal;
            Tier5Color = NameColorNormal;
            Tier10Color = NameColorNormal;

            Overhang = OverhangStyle.Clip;
            Style = ImageStyle.ThreeD;
            TankColorizeLight = Color.FromArgb(0, 128, 0, 0);
            TankColorizeMedium = Color.FromArgb(0, 128, 0, 0);
            TankColorizeHeavy = Color.FromArgb(0, 128, 0, 0);
            TankColorizeDestroyer = Color.FromArgb(0, 128, 0, 0);
            TankColorizeArtillery = Color.FromArgb(0, 128, 0, 0);
            TankOpacity = 255;
            TankAlignment = 0;
        }

        public override void DrawTank(Tank tank, DrawingContext dc)
        {
            PixelRect nameSize = new PixelRect(), tierSize = new PixelRect();

            var nameFont = new D.Font("Arial", 8.5f);
            var nameBrush = new D.SolidBrush(tank.Category.Pick(NameColorNormal, NameColorPremium, NameColorSpecial).ToColorGdi());
            var nameLayer = Ut.NewBitmapGdi((D.Graphics g) =>
            {
                g.TextRenderingHint = NameAntiAlias.ToGdi();
                nameSize = g.DrawString(tank[NameData], nameFont, nameBrush, right: 80 - 4, bottom: 24 - 3, baseline: true);
            });
            nameLayer.DrawImage(nameLayer.GetOutline(NameAntiAlias == TextAntiAliasStyle.Aliased ? 255 : 180));
            nameLayer = nameLayer.GetBlurred().DrawImage(nameLayer);

            var tierFont = new D.Font("Arial", 8.5f);
            var tierColor = tank.Tier <= 5 ? Ut.BlendColors(Tier1Color, Tier5Color, (tank.Tier - 1) / 4.0) : Ut.BlendColors(Tier5Color, Tier10Color, (tank.Tier - 5) / 5.0);
            var tierBrush = new D.SolidBrush(tierColor.ToColorGdi());
            var tierLayer = Ut.NewBitmapGdi((D.Graphics g) =>
            {
                g.TextRenderingHint = TierAntiAlias.ToGdi();
                tierSize = g.DrawString(tank.Tier.ToString(), tierFont, tierBrush, left: 3, top: 4);
            });
            tierLayer.DrawImage(tierLayer.GetOutline(TierAntiAlias == TextAntiAliasStyle.Aliased ? 255 : 180));
            tierLayer = tierLayer.GetBlurred().DrawImage(tierLayer);

            if (Style != ImageStyle.None && TankOpacity > 0)
            {
                var image = Style == ImageStyle.Contour ? tank.LoadImageContourWpf() : tank.LoadImage3DWpf();
                if (image == null)
                    tank.AddWarning("The image for this tank is missing.");
                else
                {
                    var minmax = Ut.PreciseWidth(image, 100);

                    var colorize = ColorHSV.FromColor(tank.Class.Pick(TankColorizeLight, TankColorizeMedium, TankColorizeHeavy, TankColorizeDestroyer, TankColorizeArtillery));
                    image.Colorize(colorize.Hue, colorize.Saturation / 100.0, colorize.Value / 100.0 - 0.5, colorize.Alpha / 255.0);
                    if (TankOpacity < 255)
                        image.Transparentize(TankOpacity);

                    if (Overhang != OverhangStyle.Overhang)
                        dc.PushClip(new RectangleGeometry(new Rect(1, 2, 78, 20)));
                    else if (Style == ImageStyle.ThreeD)
                    {
                        // Fade out the top and bottom couple of pixels
                        unsafe
                        {
                            byte* ptr, end;
                            int h = image.PixelHeight;
                            ptr = (byte*) image.BackBuffer + 0 * image.BackBufferStride + 3; end = ptr + image.PixelWidth * 4; for (; ptr < end; ptr += 4) *ptr = (byte) (*ptr * 0.25);
                            ptr = (byte*) image.BackBuffer + 1 * image.BackBufferStride + 3; end = ptr + image.PixelWidth * 4; for (; ptr < end; ptr += 4) *ptr = (byte) (*ptr * 0.6);
                            ptr = (byte*) image.BackBuffer + (h - 2) * image.BackBufferStride + 3; end = ptr + image.PixelWidth * 4; for (; ptr < end; ptr += 4) *ptr = (byte) (*ptr * 0.6);
                            ptr = (byte*) image.BackBuffer + (h - 1) * image.BackBufferStride + 3; end = ptr + image.PixelWidth * 4; for (; ptr < end; ptr += 4) *ptr = (byte) (*ptr * 0.25);
                        }
                    }

                    double height = Overhang == OverhangStyle.Fit ? 20 : 24;
                    double scale = height / image.Height;
                    double x;
                    if (TankAlignment == 0)
                        x = Math.Min(Math.Max((tierSize.Right + nameSize.Left) / 2 - scale * minmax.CenterHorz, 10 - minmax.Left * scale), 79 - minmax.Right * scale);
                    else if (TankAlignment == 1)
                        x = 40 - scale * minmax.CenterHorz;
                    else if (TankAlignment > 0)
                        x = TankAlignment - 2 - scale * minmax.Left;
                    else
                        x = 80 - scale * minmax.Right + TankAlignment;

                    dc.DrawImage(image, new Rect(
                        x, Overhang == OverhangStyle.Fit ? 2 : 0,
                        image.Width * scale, height));
                    if (Overhang != OverhangStyle.Overhang)
                        dc.Pop();
                }
            }

            dc.DrawImage(nameLayer);
            dc.DrawImage(tierLayer);
        }
    }

}
