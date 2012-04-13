﻿using System.Windows;
using System.Windows.Controls;
using RT.Util.Forms;

namespace TankIconMaker
{
    partial class AddWindow : ManagedWindow
    {
        public AddWindow()
            : base(Program.Settings.AddWindow)
        {
            InitializeComponent();
            ContentRendered += delegate
            {
                ((ListBoxItem) ctList.ItemContainerGenerator.ContainerFromIndex(0)).Focus();
            };
        }

        private void add(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public static LayerBase ShowAddLayer(Window owner)
        {
            var wnd = new AddWindow { Owner = owner };
            wnd.Title = "Add layer";
            wnd.lblName.Visibility = Visibility.Visible;
            wnd.ctName.Visibility = Visibility.Visible;
            wnd.lblList.Content = "Layer _type:";
            wnd.ctList.ItemsSource = Program.LayerTypes;
            wnd.ctList.SelectedIndex = 0;

            if (wnd.ShowDialog() != true)
                return null;

            var item = wnd.ctList.SelectedItem as TypeInfo<LayerBase>;
            if (item == null)
                return null;
            var result = item.Constructor();
            result.Name = wnd.ctName.Text;
            return result;
        }

        public static EffectBase ShowAddEffect(Window owner)
        {
            var wnd = new AddWindow { Owner = owner };
            wnd.Title = "Add effect";
            wnd.lblName.Visibility = Visibility.Collapsed;
            wnd.ctName.Visibility = Visibility.Collapsed;
            wnd.lblList.Content = "Effect _type:";
            wnd.ctList.ItemsSource = Program.EffectTypes;
            wnd.ctList.SelectedIndex = 0;

            if (wnd.ShowDialog() != true)
                return null;

            var item = wnd.ctList.SelectedItem as TypeInfo<EffectBase>;
            if (item == null)
                return null;
            return item.Constructor();
        }
    }
}
