﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ookii.Dialogs.Wpf;
using RT.Util;
using RT.Util.Dialogs;
using RT.Util.Xml;

/*
 * Refactor property resolution into Data.cs
 * Implement a way to choose preferred built-in property author
 * Provide a means to load the in-game images
 * Provide a means to load user-supplied images
 * Load/save sets of properties to XML files (make sure distribution is well-supported)
 * Bundled properties:
 *     Override example for russian colloquial names
 * "Reload data" button
 * 
 * Good handling of exceptions in the maker: show a graphic for the failed tank; show what's wrong on click. Detect common errors like the shared resource usage exception
 * Good handling of exceptions due to bugs in the program (show detail and exit)
 * Good handling of when the bare minimum data files are missing (e.g. at least one BuitlIn and at least one GameVersion)
 * Report file loading errors properly
 * Test-render a tank with all null properties and tell the user if this fails (and deduce which property fails)
 * Same method to draw text with GDI (various anti-aliasing settings) and WPF (another item in the anti-alias enum)
 * Deduce the text baseline in pixel-perfect fashion.
 * 
 * Use a drop-down listing all possible properties for NameDataSource
 * In-game-like display of low/mid/high tier balance
 * Allow the maker to tell us which tanks to invalidate on a property change.
 */

/*
 * Inheritance use-cases:
 *   Definitely: override a few properties from someone else's data, but for new version be able to import their new file with your overrides
 */

namespace TankIconMaker
{
    partial class MainWindow : ManagedWindow
    {
        private string _exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private List<DataFileBuiltIn> _builtin = new List<DataFileBuiltIn>();
        private List<DataFileExtra> _extra = new List<DataFileExtra>();
        private Dictionary<Version, GameVersion> _versions = new Dictionary<Version, GameVersion>();
        private List<MakerBase> _makers = new List<MakerBase>();
        private DispatcherTimer _updateIconsTimer = new DispatcherTimer(DispatcherPriority.Background);
        private CancellationTokenSource _cancelRender = new CancellationTokenSource();
        private Dictionary<string, BitmapSource> _renderCache = new Dictionary<string, BitmapSource>();

        public MainWindow()
            : base(Program.Settings.MainWindow)
        {
            InitializeComponent();
            _updateIconsTimer.Tick += UpdateIcons;
            _updateIconsTimer.Interval = TimeSpan.FromMilliseconds(100);

            GlobalStatusShow("Loading...");

            BindingOperations.SetBinding(ctRemoveGamePath, Button.IsEnabledProperty, new Binding
            {
                Source = ctGamePath,
                Path = new PropertyPath(ComboBox.SelectedIndexProperty),
                Converter = LambdaConverter.New((int index) => index >= 0),
            });
            BindingOperations.SetBinding(ctGameVersion, ComboBox.IsEnabledProperty, new Binding
            {
                Source = ctGamePath,
                Path = new PropertyPath(ComboBox.SelectedIndexProperty),
                Converter = LambdaConverter.New((int index) => index >= 0),
            });

            if (Program.Settings.LeftColumnWidth != null)
                ctLeftColumn.Width = new GridLength(Program.Settings.LeftColumnWidth.Value);
            if (Program.Settings.NameColumnWidth != null)
                ctMakerProperties.NameColumnWidth = Program.Settings.NameColumnWidth.Value;
            if (Program.Settings.DisplayMode >= 0 && Program.Settings.DisplayMode < ctDisplayMode.Items.Count)
                ctDisplayMode.SelectedIndex = Program.Settings.DisplayMode.Value;
            ctGamePath.ItemsSource = Program.Settings.GameInstalls;
            ctGamePath.DisplayMemberPath = "DisplayName";
            ctGamePath.SelectedItem = Program.Settings.GameInstalls.FirstOrDefault(gis => gis.Path.EqualsNoCase(Program.Settings.SelectedGamePath))
                ?? Program.Settings.GameInstalls.FirstOrDefault();

            ContentRendered += InitializeEverything;
        }

        private void GlobalStatusShow(string message)
        {
            (ctGlobalStatusBox.Child as TextBlock).Text = message;
            ctGlobalStatusBox.Visibility = Visibility.Visible;
            IsEnabled = false;
            ctIconsPanel.Opacity = 0.6;
        }

        private void GlobalStatusHide()
        {
            IsEnabled = true;
            ctGlobalStatusBox.Visibility = Visibility.Collapsed;
            ctIconsPanel.Opacity = 1;
        }

        void InitializeEverything(object _, EventArgs __)
        {
            ContentRendered -= InitializeEverything;

            if (File.Exists(Path.Combine(_exePath, "background.jpg")))
                ctOuterGrid.Background = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(Path.Combine(_exePath, "background.jpg"))),
                    Stretch = Stretch.UniformToFill,
                };

            ReloadData();

            // Find all the makers
            foreach (var makerType in Assembly.GetEntryAssembly().GetTypes().Where(t => typeof(MakerBase).IsAssignableFrom(t) && !t.IsAbstract))
            {
                var constructor = makerType.GetConstructor(new Type[0]);
                if (constructor == null)
                {
                    DlgMessage.ShowWarning("Ignoring maker type \"{0}\" because it does not have a public parameterless constructor.".Fmt(makerType));
                    continue;
                }
                var maker = (MakerBase) constructor.Invoke(new object[0]);
                _makers.Add(maker);
            }

            _makers = _makers.OrderBy(m => m.Name).ThenBy(m => m.Author).ThenBy(m => m.Version).ToList();

            // Put the makers into the maker dropdown
            foreach (var maker in _makers)
                ctMakerDropdown.Items.Add(maker);

            // Locate the closest match for the maker that was selected last time the program was run
            ctMakerDropdown.SelectedItem = _makers
                .OrderBy(m => m.GetType().FullName == Program.Settings.SelectedMakerType ? 0 : 1)
                .ThenBy(m => m.Name == Program.Settings.SelectedMakerName ? 0 : 1)
                .ThenBy(m => _makers.IndexOf(m))
                .First();

            // Yes, this stuff is a bit WinForms'sy...
            var gis = GetInstallationSettings(addIfMissing: true);
            ctGameVersion.Text = gis.GameVersion;
            ctMakerDropdown_SelectionChanged();

            // Bind the events now that all the UI is set up as desired
            Closing += (___, ____) => SaveSettings();
            this.SizeChanged += SaveSettings;
            this.LocationChanged += SaveSettings;
            ctMakerDropdown.SelectionChanged += ctMakerDropdown_SelectionChanged;
            ctMakerProperties.PropertyChanged += ctMakerProperties_PropertyChanged;
            ctDisplayMode.SelectionChanged += ctDisplayMode_SelectionChanged;
            ctGameVersion.SelectionChanged += ctGameVersion_SelectionChanged;
            ctGamePath.SelectionChanged += ctGamePath_SelectionChanged;
            ctGamePath.PreviewKeyDown += ctGamePath_PreviewKeyDown;

            // Done
            GlobalStatusHide();
            _updateIconsTimer.Start();
        }

        private void ReloadData()
        {
            _renderCache.Clear();

            // Read data files off disk
            var builtin = new List<DataFileBuiltIn>();
            var extra = new List<DataFileExtraWithInherit>();
            var origFilenames = new Dictionary<object, string>();
            foreach (var fi in new DirectoryInfo(_exePath).GetFiles("Data-*.csv"))
            {
                var parts = fi.Name.Substring(0, fi.Name.Length - 4).Split('-');
                var partsr = parts.Reverse().ToArray();

                if (parts.Length < 5 || parts.Length > 6)
                {
                    Console.WriteLine("Skipping \"{0}\" because it has the wrong number of filename parts.", fi.Name);
                    continue;
                }
                if (parts[1].EqualsNoCase("BuiltIn") && parts.Length != 5)
                {
                    Console.WriteLine("Skipping \"{0}\" because it has too many filename parts for a BuiltIn data file.", fi.Name);
                    continue;
                }
                if (parts.Length == 5 && !parts[1].EqualsNoCase("BuiltIn"))
                {
                    Console.WriteLine("Skipping \"{0}\" because it has too few filename parts for a non-BuiltIn data file.", fi.Name);
                    continue;
                }

                string author = partsr[2].Trim();
                if (author.Length == 0)
                {
                    Console.WriteLine("Skipping \"{0}\" because it has an empty author part in the filename.", fi.Name);
                    continue;
                }

                Version gameVersion;
                if (!Version.TryParse(partsr[1], out gameVersion))
                {
                    Console.WriteLine("Skipping \"{0}\" because it has an unparseable game version part in the filename: \"{1}\".", fi.Name, partsr[1]);
                    continue;
                }

                int fileVersion;
                if (!int.TryParse(partsr[0], out fileVersion))
                {
                    Console.WriteLine("Skipping \"{0}\" because it has an unparseable file version part in the filename: \"{1}\".", fi.Name, partsr[0]);
                    continue;
                }

                if (parts.Length == 5)
                {
                    var df = new DataFileBuiltIn(author, gameVersion, fileVersion, fi.FullName);
                    builtin.Add(df);
                    origFilenames[df] = fi.Name;
                }
                else
                {
                    string extraName = parts[1].Trim();
                    if (extraName.Length == 0)
                    {
                        Console.WriteLine("Skipping \"{0}\" because it has an empty property name part in the filename.", fi.Name);
                        continue;
                    }

                    string languageName = parts[2].Trim();
                    if (languageName.Length != 2)
                    {
                        Console.WriteLine("Skipping \"{0}\" because its language name part in the filename is not a 2 letter long language code.", fi.Name);
                        continue;
                    }

                    var df = new DataFileExtraWithInherit(extraName, languageName, author, gameVersion, fileVersion, fi.FullName);
                    extra.Add(df);
                    origFilenames[df] = fi.Name;
                }
            }

            // Resolve built-in data files
            _builtin.Clear();
            foreach (var group in builtin.GroupBy(df => new { author = df.Author, gamever = df.GameVersion }).OrderBy(g => g.Key.gamever))
            {
                var tanks = new Dictionary<string, TankData>();
                // Inherit from the earlier game versions by same author
                var earlierVer = _builtin.Where(df => df.Author == group.Key.author).OrderByDescending(df => df.GameVersion).FirstOrDefault();
                if (earlierVer != null)
                    foreach (var row in earlierVer.Data)
                        tanks[row.SystemId] = row;
                // Inherit from all the data files by this author/game version
                foreach (var row in group.OrderBy(df => df.FileVersion).SelectMany(df => df.Data))
                    tanks[row.SystemId] = row;
                // Create a new data file with all the tanks
                _builtin.Add(new DataFileBuiltIn(group.Key.author, group.Key.gamever, group.Max(df => df.FileVersion), tanks.Values));
            }

            // Make sure the explicit inheritance is resolvable, and complain if not
            var ignore = new List<DataFileExtra>();
            do
            {
                ignore.Clear();
                foreach (var e in extra.Where(e => e.InheritsFromName != null))
                {
                    var p = extra.Where(df => df.Name == e.InheritsFromName).ToList();
                    if (p.Count == 0)
                    {
                        Console.WriteLine("Skipping \"{0}\" because there are no data files for the property \"{1}\" (from which it inherits values).".Fmt(origFilenames[e], e.InheritsFromName));
                        ignore.Add(e);
                        continue;
                    }
                    if (e.InheritsFromLanguage != null)
                    {
                        p = p.Where(df => df.Language == e.InheritsFromLanguage).ToList();
                        if (p.Count == 0)
                        {
                            Console.WriteLine("Skipping \"{0}\" because no data files for the property \"{1}\" (from which it inherits values) are in language \"{2}\"".Fmt(origFilenames[e], e.InheritsFromName, e.InheritsFromLanguage));
                            ignore.Add(e);
                            continue;
                        }
                    }
                    p = p.Where(df => df.GameVersion <= e.GameVersion).ToList();
                    if (p.Count == 0)
                    {
                        Console.WriteLine("Skipping \"{0}\" because no data files for the property \"{1}\"/\"{2}\" (from which it inherits values) have game version \"{3}\" or below.".Fmt(origFilenames[e], e.InheritsFromName, e.InheritsFromLanguage, e.GameVersion));
                        ignore.Add(e);
                        continue;
                    }
                }
                extra.RemoveAll(f => ignore.Contains(f));
            } while (ignore.Count > 0);

            // Determine all the immediate parents
            foreach (var e in extra)
            {
                var sameNEL = extra.Where(df => df.Name == e.Name && df.Author == e.Author && df.Language == e.Language).ToList();

                // Inherit from an earlier version of this same file
                var earlierVersionOfSameFile = sameNEL.Where(df => df.GameVersion == e.GameVersion && df.FileVersion < e.FileVersion)
                    .MaxOrDefault(df => df.FileVersion);
                if (earlierVersionOfSameFile != null)
                    e.ImmediateParents.Add(earlierVersionOfSameFile);

                // Inherit from the latest version of the same file for an earlier game version
                var earlierGameVersion = sameNEL.Where(df => df.GameVersion < e.GameVersion).MaxAll(df => df.GameVersion).MaxOrDefault(df => df.FileVersion);
                if (earlierGameVersion != null)
                    e.ImmediateParents.Add(earlierGameVersion);

                // Inherit from the explicitly specified file
                if (e.InheritsFromName != null)
                {
                    var p = extra.Where(df => df.GameVersion <= e.GameVersion && df.Name == e.InheritsFromName).ToList();
                    if (e.InheritsFromLanguage != null)
                        p = p.Where(df => df.Language == e.InheritsFromLanguage).ToList();
                    e.ImmediateParents.Add(p.Where(df => df.Author == e.InheritsFromAuthor).FirstOrDefault() ?? p[0]);
                }
            }

            // Compute the transitive closure
            bool added;
            foreach (var e in extra)
                foreach (var p in e.ImmediateParents)
                    p.TransitiveChildren.Add(e);
            // Keep adding children's children until no further changes (quite a brute-force algorithm... potential bottleneck)
            do
            {
                added = false;
                foreach (var e in extra)
                    foreach (var c1 in e.TransitiveChildren)
                        foreach (var c2 in c1.TransitiveChildren)
                            if (!c1.TransitiveChildren.Contains(c2))
                            {
                                c1.TransitiveChildren.Add(c2);
                                added = true;
                            }
            } while (added);

            // Detect dependency loops and remove them
            var looped = extra.Where(e => e.TransitiveChildren.Contains(e)).ToArray();
            foreach (var item in looped.ToArray())
            {
                Console.WriteLine("Skipping \"{0}\" due to a circular dependency.".Fmt(origFilenames[item]));
                extra.Remove(item);
            }

            // Get the full list of properties for every data file
            foreach (var e in extra.OrderBy(df => df, new CustomComparer<DataFileExtraWithInherit>((df1, df2) => df1.TransitiveChildren.Contains(df2) ? -1 : df2.TransitiveChildren.Contains(df1) ? 1 : 0)))
            {
                var tanks = new Dictionary<string, ExtraData>();

                // Inherit the properties (all the hard work is already done and the files to inherit from are in the correct order)
                foreach (var p in e.ImmediateParents)
                    foreach (var d in p.Result.Data)
                        tanks[d.TankSystemId] = d;
                foreach (var d in e.Data)
                    tanks[d.TankSystemId] = d;

                // Create a new data file with all the tanks
                e.Result = new DataFileExtra(e.Name, e.Language, e.Author, e.GameVersion, e.FileVersion, tanks.Values);
            }

            // Keep only the latest file version of each file
            _extra.Clear();
            foreach (var e in extra.GroupBy(df => new { name = df.Name, language = df.Language, author = df.Author, gamever = df.GameVersion }))
                _extra.Add(e.Single(k => k.FileVersion == e.Max(m => m.FileVersion)).Result);

            // Read game versions off disk
            _versions.Clear();
            foreach (var fi in new DirectoryInfo(_exePath).GetFiles("GameVersion-*.xml"))
            {
                var parts = fi.Name.Substring(0, fi.Name.Length - 4).Split('-');

                if (parts.Length != 2)
                {
                    Console.WriteLine("Skipping \"{0}\" because it has the wrong number of filename parts.", fi.Name);
                    continue;
                }

                Version gameVersion;
                if (!Version.TryParse(parts[1], out gameVersion))
                {
                    Console.WriteLine("Skipping \"{0}\" because it has an unparseable game version part in the filename: \"{1}\".", fi.Name, parts[1]);
                    continue;
                }

                try
                {
                    _versions.Add(gameVersion, XmlClassify.LoadObjectFromXmlFile<GameVersion>(fi.FullName));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Skipping \"{0}\" because the file could not be parsed: {1}", fi.Name, e.Message);
                    continue;
                }
            }

            // Refresh game versions UI (TODO: just use binding)
            ctGameVersion.Items.Clear();
            foreach (var key in _versions.Keys.OrderBy(v => v))
                ctGameVersion.Items.Add(key);
            GetInstallationSettings(); // fixes up the versions if necessary
        }

        /// <summary>
        /// Schedules an icon update to occur after a short timeout. If called again before the timeout, will re-set the timeout
        /// back to original value. If called during a render, the render is cancelled immediately. Call this if the event that
        /// invalidated the current icons can occur frequently. Call <see cref="UpdateIcons"/> for immediate response.
        /// </summary>
        private void ScheduleUpdateIcons()
        {
            _cancelRender.Cancel();

            ctSave.IsEnabled = false;
            foreach (var image in ctIconsPanel.Children.OfType<Image>())
                image.Opacity = 0.7;

            _updateIconsTimer.Stop();
            _updateIconsTimer.Start();
        }

        /// <summary>
        /// Begins an icon update immediately. The icons are rendered in the background without blocking the UI. If called during
        /// a previous render, the render is cancelled immediately. Call this if the event that invalidated the current icons occurs
        /// infrequently, to ensure immediate response to user action. For very frequent updates, use <see cref="ScheduleUpdateIcons"/>.
        /// Only the icons not in the render cache are re-rendered; remove some or all to force a re-render of the icon.
        /// </summary>
        private void UpdateIcons(object _ = null, EventArgs __ = null)
        {
            _updateIconsTimer.Stop();
            _cancelRender.Cancel();
            _cancelRender = new CancellationTokenSource();
            var cancelToken = _cancelRender.Token; // must be a local so that the task lambda captures it; _cancelRender could get reassigned before a task gets to check for cancellation of the old one

            var maker = (MakerBase) ctMakerDropdown.SelectedItem;
            maker.Initialize();

            var images = ctIconsPanel.Children.OfType<Image>().ToList();
            var tanks = EnumTanks().ToList();

            var tasks = new List<Action>();
            for (int i = 0; i < tanks.Count; i++)
            {
                if (i >= images.Count)
                {
                    var img = new Image
                    {
                        SnapsToDevicePixels = true,
                        Margin = new Thickness { Right = 15 },
                        Cursor = Cursors.Hand,
                        Opacity = 0.7,
                    };
                    BindingOperations.SetBinding(img, Image.WidthProperty, new Binding
                    {
                        Source = ctZoomCheckbox,
                        Path = new PropertyPath(CheckBox.IsCheckedProperty),
                        Converter = LambdaConverter.New((bool check) => 80 * (check ? 5 : 1)),
                    });
                    BindingOperations.SetBinding(img, Image.HeightProperty, new Binding
                    {
                        Source = ctZoomCheckbox,
                        Path = new PropertyPath(CheckBox.IsCheckedProperty),
                        Converter = LambdaConverter.New((bool check) => 24 * (check ? 5 : 1)),
                    });
                    ctIconsPanel.Children.Add(img);
                    images.Add(img);
                }
                var tank = tanks[i];
                var image = images[i];

                image.ToolTip = tanks[i].SystemId + (tanks[i]["OfficialName"] == null ? "" : (" (" + tanks[i]["OfficialName"] + ")"));
                if (_renderCache.ContainsKey(tank.SystemId))
                {
                    image.Source = _renderCache[tank.SystemId];
                    image.Opacity = 1;
                }
                else
                    tasks.Add(() =>
                    {
                        try
                        {
                            if (cancelToken.IsCancellationRequested) return;
                            var source = maker.DrawTankInternal(tank);
                            if (cancelToken.IsCancellationRequested) return;
                            Dispatcher.Invoke(new Action(() =>
                            {
                                if (cancelToken.IsCancellationRequested) return;
                                _renderCache[tank.SystemId] = source;
                                image.Source = source;
                                image.Opacity = 1;
                                if (ctIconsPanel.Children.OfType<Image>().All(c => c.Opacity == 1))
                                    UpdateIconsCompleted();
                            }));
                        }
                        catch { } // will do something more appropriate later
                    });
            }
            foreach (var task in tasks)
                Task.Factory.StartNew(task, cancelToken);

            // Remove unused images
            foreach (var image in images.Skip(tanks.Count))
                ctIconsPanel.Children.Remove(image);
        }

        /// <summary>
        /// Called on the GUI thread whenever all the icon renders are completed.
        /// </summary>
        private void UpdateIconsCompleted()
        {
            ctSave.IsEnabled = true;
        }

        private void SaveSettings()
        {
            Program.Settings.LeftColumnWidth = ctLeftColumn.Width.Value;
            Program.Settings.NameColumnWidth = ctMakerProperties.NameColumnWidth;
            Program.Settings.SaveThreaded();
        }

        private void SaveSettings(object _, SizeChangedEventArgs __)
        {
            SaveSettings();
        }

        private void SaveSettings(object _, EventArgs __)
        {
            SaveSettings();
        }

        private void ctMakerDropdown_SelectionChanged(object _ = null, SelectionChangedEventArgs __ = null)
        {
            _renderCache.Clear();
            ScheduleUpdateIcons();
            var maker = (MakerBase) ctMakerDropdown.SelectedItem;
            ctMakerProperties.SelectedObject = maker;
            ctMakerDescription.Text = maker.Description ?? "";
            Program.Settings.SelectedMakerType = maker.GetType().FullName;
            Program.Settings.SelectedMakerName = maker.Name;
            SaveSettings();
        }

        private IEnumerable<Tank> EnumTanks(bool all = false)
        {
            var gis = GetInstallationSettings();
            var selectedVersion = Version.Parse(gis.GameVersion);

            IEnumerable<TankData> alls = _builtin.Where(b => b.GameVersion.ToString() == gis.GameVersion).First().Data;
            IEnumerable<TankData> selection = null;

            if (all || ctDisplayMode.SelectedIndex == 0) // all tanks
                selection = alls;
            else if (ctDisplayMode.SelectedIndex == 1) // one of each
                selection = alls.Select(t => new { t.Category, t.Class, t.Country }).Distinct()
                    .SelectMany(p => SelectTiers(alls.Where(t => t.Category == p.Category && t.Class == p.Class && t.Country == p.Country)));

            var extras = _extra.GroupBy(df => new { df.Name, df.Language, df.Author })
                .Select(g => g.Where(df => df.GameVersion <= selectedVersion).MaxOrDefault(df => df.GameVersion))
                .Where(df => df != null).ToList();
            return selection.OrderBy(t => t.Country).ThenBy(t => t.Class).ThenBy(t => t.Tier).ThenBy(t => t.Category).ThenBy(t => t.SystemId)
                .Select(tank => new Tank(
                    tank,
                    extras.Select(df => new KeyValuePair<string, string>(
                        key: df.Name + " - " + df.Language + " - " + df.Author,
                        value: df.Data.Where(dp => dp.TankSystemId == tank.SystemId).Select(dp => dp.Value).FirstOrDefault()
                    ))
                )).ToList();
        }

        private IEnumerable<TankData> SelectTiers(IEnumerable<TankData> tanks)
        {
            TankData min = null;
            TankData mid = null;
            TankData max = null;
            foreach (var tank in tanks)
            {
                if (min == null || tank.Tier < min.Tier)
                    min = tank;
                if (mid == null || Math.Abs(tank.Tier - 5) < Math.Abs(mid.Tier - 5))
                    mid = tank;
                if (max == null || tank.Tier > max.Tier)
                    max = tank;
            }
            if (Math.Abs((mid == null ? 999 : mid.Tier) - (min == null ? 999 : min.Tier)) < 3)
                mid = null;
            if (Math.Abs((mid == null ? 999 : mid.Tier) - (max == null ? 999 : max.Tier)) < 3)
                mid = null;
            if (Math.Abs((min == null ? 999 : min.Tier) - (max == null ? 999 : max.Tier)) < 5)
                max = null;
            if (min != null)
                yield return min;
            if (mid != null)
                yield return mid;
            if (max != null)
                yield return max;
        }

        private void ctMakerProperties_PropertyChanged(object _, RoutedEventArgs __)
        {
            _renderCache.Clear();
            ScheduleUpdateIcons();
        }

        private void ctGameVersion_SelectionChanged(object _, SelectionChangedEventArgs args)
        {
            var gis = GetInstallationSettings();
            if (gis == null)
                return;
            gis.GameVersion = args.AddedItems.OfType<Version>().FirstOrDefault().ToString();
            ctGamePath.SelectedItem = gis;
            SaveSettings();
            _renderCache.Clear();
            ScheduleUpdateIcons();
        }

        void ctGamePath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var gis = GetInstallationSettings();
            if (gis == null)
                return;
            ctGameVersion.Text = gis.GameVersion;
        }

        void ctGamePath_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ctGamePath.IsKeyboardFocusWithin && ctGamePath.IsDropDownOpen && e.Key == Key.Delete)
            {
                RemoveGameDirectory();
                e.Handled = true;
            }
        }

        private void ctDisplayMode_SelectionChanged(object _, SelectionChangedEventArgs __)
        {
            Program.Settings.DisplayMode = ctDisplayMode.SelectedIndex;
            UpdateIcons();
            SaveSettings();
        }

        bool _overwriteAccepted = false;

        private void ctSave_Click(object _, RoutedEventArgs __)
        {
            var gis = GetInstallationSettings();
            if (gis == null)
            {
                DlgMessage.ShowInfo("Please add a game path first (top left, green plus button) so that TankIconMaker knows where to save them.");
                return;
            }

            if (!EnsureBackup())
                return;

            var path = GetIconDestinationPath();
            if (!_overwriteAccepted)
                if (DlgMessage.ShowQuestion("Would you like to overwrite your current icons?\n\nPath: {0}\n\nWarning: ALL .tga files in this path will be overwritten, and there is NO UNDO for this!"
                    .Fmt(path), "&Yes, overwrite all files", "&Cancel") == 1)
                    return;
            _overwriteAccepted = true;

            GlobalStatusShow("Saving...");

            var maker = (MakerBase) ctMakerDropdown.SelectedItem;
            var tanks = EnumTanks(all: true).ToList();
            var renders = _renderCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    foreach (var tank in tanks)
                        if (!renders.ContainsKey(tank.SystemId))
                            renders[tank.SystemId] = maker.DrawTankInternal(tank);
                    foreach (var kvp in renders)
                        Targa.Save(kvp.Value, Path.Combine(path, kvp.Key + ".tga"));
                }
                finally
                {
                    Dispatcher.Invoke((Action) GlobalStatusHide);
                }

                Dispatcher.Invoke((Action) (() =>
                {
                    foreach (var kvp in renders)
                        if (!_renderCache.ContainsKey(kvp.Key))
                            _renderCache[kvp.Key] = kvp.Value;
                    DlgMessage.ShowInfo("Saved!\nEnjoy.");
                }));
            });
        }

        private bool EnsureBackup()
        {
            try
            {
                IEnumerable<FileInfo> copy;
                var path = GetIconDestinationPath();
                var pathOriginal = Path.Combine(path, "original");
                var current = new DirectoryInfo(path).GetFiles("*.tga");
                if (Directory.Exists(pathOriginal))
                {
                    var original = new DirectoryInfo(pathOriginal).GetFiles("*.tga");
                    copy = current.Except(original, CustomEqualityComparer<FileInfo>.By(di => di.Name, ignoreCase: true));
                }
                else
                {
                    if (DlgMessage.ShowInfo("TankIconMaker needs to make a backup of your original icons, in case you want them back.\n\nPath: {0}\n\nProceed?"
                        .Fmt(pathOriginal), "&Make backup", "&Cancel") == 1)
                        return false;
                    copy = current;
                }

                Directory.CreateDirectory(pathOriginal);
                foreach (var file in copy)
                    file.CopyTo(Path.Combine(pathOriginal, file.Name));

                _overwriteAccepted = true;
                return true;
            }
            catch (Exception e)
            {
                DlgMessage.ShowError("Could not check / create backup of the original icons. Please tell the developer!\n\nError details: {0} ({1})."
                    .Fmt(e.Message, e.GetType().Name));
                return false;
            }
        }

        private void BrowseForGameDirectory(object _, RoutedEventArgs __)
        {
            var dlg = new VistaFolderBrowserDialog();
            var gis = GetInstallationSettings();
            if (gis != null && Directory.Exists(gis.Path))
                dlg.SelectedPath = gis.Path;
            if (dlg.ShowDialog() != true)
                return;

            var best = _versions.Where(v => File.Exists(Path.Combine(dlg.SelectedPath, v.Value.CheckFileName))).ToList();
            if (best.Count == 0)
            {
                if (DlgMessage.ShowWarning("This directory does not appear to contain a World Of Tanks installation. Are you sure you want to use it anyway?",
                    "&Use anyway", "Cancel") == 1)
                    return;
            }
            var version = best.Where(v => new FileInfo(Path.Combine(dlg.SelectedPath, v.Value.CheckFileName)).Length == v.Value.CheckFileSize)
                .Select(v => v.Key)
                .OrderByDescending(v => v)
                .FirstOrDefault();

            gis = new GameInstallationSettings { Path = dlg.SelectedPath, GameVersion = version == null ? _versions.Keys.Max().ToString() : version.ToString() };
            Program.Settings.GameInstalls.Add(gis);
            ctGamePath.SelectedItem = gis;
            Program.Settings.SaveThreaded();
        }

        private void RemoveGameDirectory(object _ = null, RoutedEventArgs __ = null)
        {
            // Looks rather hacky but seems to do the job correctly even when called with the drop-down visible.
            var index = ctGamePath.SelectedIndex;
            Program.Settings.GameInstalls.RemoveAt(ctGamePath.SelectedIndex);
            ctGamePath.ItemsSource = null;
            ctGamePath.ItemsSource = Program.Settings.GameInstalls;
            ctGamePath.SelectedIndex = Math.Min(index, Program.Settings.GameInstalls.Count - 1);
            SaveSettings();
        }

        private GameInstallationSettings GetInstallationSettings(bool addIfMissing = false)
        {
            var gis = ctGamePath.SelectedItem as GameInstallationSettings;
            if (gis == null)
            {
                if (!addIfMissing)
                    return null;
                gis = new GameInstallationSettings { Path = Ut.FindTanksDirectory(), GameVersion = _versions.Keys.Max().ToString() };
                Program.Settings.GameInstalls.Add(gis);
                ctGamePath.SelectedItem = gis;
                ctGamePath.Items.Refresh();
                Program.Settings.SaveThreaded();
            }

            Version v;
            if (!Version.TryParse(gis.GameVersion, out v) || !_versions.ContainsKey(v))
            {
                gis.GameVersion = ctGameVersion.Text = _versions.Keys.Max().ToString();
                ctGamePath.Items.Refresh();
                Program.Settings.SaveThreaded();
            }

            return gis;
        }

        private string GetIconDestinationPath()
        {
            var gis = GetInstallationSettings();
            if (gis == null)
                return null;
            return Path.Combine(gis.Path, _versions[Version.Parse(gis.GameVersion)].PathDestination);
        }

        private string GetIconSource3DPath()
        {
            var gis = GetInstallationSettings();
            if (gis == null)
                return null;
            return Path.Combine(gis.Path, _versions[Version.Parse(gis.GameVersion)].PathSource3D);
        }
    }
}
