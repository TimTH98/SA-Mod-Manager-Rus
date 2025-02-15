﻿using SAModManager.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using SAModManager.Configuration.SA2;
using SAModManager.Management;

namespace SAModManager.Controls.SA2
{
	/// <summary>
	/// Interaction logic for GameConfig.xaml
	/// </summary>
	public partial class GameConfig : UserControl
	{
        #region Variables
        public GameSettings GameProfile;

        bool suppressEvent = false;
        private static string patchesPath = null;
        #endregion

        public GameConfig(ref object gameSettings, ref bool suppressEvent_)
        {
            InitializeComponent();
            suppressEvent = suppressEvent_;
            GameProfile = (GameSettings)gameSettings;
            if (App.CurrentGame?.modDirectory != null && Directory.Exists(App.CurrentGame.modDirectory))
            {
                string pathDest = Path.Combine(App.CurrentGame.modDirectory, "Patches.json");
                if (File.Exists(pathDest))
                    patchesPath = pathDest;
                SetPatches();
            }
            Loaded += GameConfig_Loaded;
        }

        #region Internal Functions
        private void GameConfig_Loaded(object sender, RoutedEventArgs e)
        {
            SetupBindings();
            SetPatches();
        }

        #region Graphics Tab
        private void ResolutionChanged(object sender, RoutedEventArgs e)
        {

            NumberBox box = sender as NumberBox;

            switch (box.Name)
            {
                case "txtResY":
                    if (chkRatio.IsChecked == true)
                    {
                        txtResX.Value = Math.Ceiling(txtResY.Value * GraphicsManager.GetRatio(GraphicsManager.Ratio.ratio43));
                    }
                    break;
                case "txtCustomResY":
                    if (chkMaintainRatio.IsChecked == true)
                    {
                        txtCustomResX.Value = Math.Ceiling(txtCustomResY.Value * GraphicsManager.GetRatio(GraphicsManager.Ratio.ratio43));
                    }
                    break;
            }

            if (!suppressEvent)
                comboDisplay.SelectedIndex = -1;

        }

        private void HorizontalRes_Changed(object sender, RoutedEventArgs e)
        {
            if (!suppressEvent)
                comboDisplay.SelectedIndex = -1;
        }

        private void comboScreen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			if (GraphicsManager.Screens.Count > 1)
				GraphicsManager.UpdateResolutionPresets(comboScreen.SelectedIndex);
		}

        private void chkRatio_Click(object sender, RoutedEventArgs e)
        {
            if (chkRatio.IsChecked == true)
            {
                txtResX.IsEnabled = false;
                decimal resYDecimal = txtResY.Value;
				Decimal roundedValue = Math.Round(resYDecimal * GraphicsManager.GetRatio(GraphicsManager.Ratio.ratio43));
                txtResX.Value = roundedValue;
            }
            else if (!suppressEvent)
            {
                txtResX.IsEnabled = true;
            }
        }

        private void DisplaySize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox box = (ComboBox)sender;

            if (box.SelectedIndex == -1)
                return;

            int index = box.SelectedIndex;

            suppressEvent = true;

            switch (box.Name)
            {
                case "comboDisplay":
                    txtResY.Value = GraphicsManager.ResolutionPresets[index].Height;

                    if (chkRatio.IsChecked == false)
                        txtResX.Value = GraphicsManager.ResolutionPresets[index].Width;
                    break;

                case "comboCustomWindow":
                    txtCustomResY.Value = GraphicsManager.ResolutionPresets[index].Height;

                    if (chkRatio.IsChecked == false)
                        txtCustomResX.Value = GraphicsManager.ResolutionPresets[index].Width;
                    break;
            }

            suppressEvent = false;
        }

        private void chkMaintainRatio_Click(object sender, RoutedEventArgs e)
        {
            if (chkMaintainRatio.IsChecked == true)
            {
                txtCustomResX.IsEnabled = false;
                decimal ratio = txtResX.Value / txtResY.Value;
                txtCustomResX.Value = Math.Ceiling(txtCustomResY.Value * ratio);
            }
            else if (!suppressEvent)
            {
                txtCustomResX.IsEnabled = true;
            }
        }

        #endregion

     
        #endregion

        #region Patches Tab
        private PatchesData GetPatchFromView(object sender)
        {
            if (sender is ListViewItem lvItem)
                return lvItem.Content as PatchesData;
            else if (sender is ListView lv)
                return lv.SelectedItem as PatchesData;


            return listPatches.Items[listPatches.SelectedIndex] as PatchesData;
        }

        private void PatchViewItem_MouseEnter(object sender, MouseEventArgs e)
        {

            var patch = GetPatchFromView(sender);

            if (patch is null)
                return;

            PatchAuthor.Text += ": " + patch.Author;
            PatchCategory.Text += ": " + patch.Category;
            PatchDescription.Text += " " + patch.Description;
        }

        private void PatchViewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            PatchAuthor.Text = Lang.GetString("CommonStrings.Author");
            PatchCategory.Text = Lang.GetString("CommonStrings.Category");
            PatchDescription.Text = Lang.GetString("CommonStrings.Description");
        }

        private static List<PatchesData> GetPatches(ref ListView list, GameSettings set)
        {
            list.Items.Clear();

            var patches = PatchesList.Deserialize(patchesPath);
            bool isListEmpty = set.EnabledGamePatches.Count == 0;

            if (patches is not null)
            {
                var listPatch = patches.Patches;

                foreach (var patch in listPatch)
                {
                    if (isListEmpty)
                    {
                        // Convert patch name to the corresponding property name in GamePatches class
                        string propertyName = patch.Name.Replace(" ", ""); // Adjust the naming convention as needed
                        var property = typeof(GamePatches).GetProperty(propertyName);

                        if (property != null)
                        {
                            // Update the IsChecked property based on the GamePatches class
                            patch.IsChecked = (bool)property.GetValue(set.Patches);
                        }
                    }
                    else
                    {
                        patch.IsChecked = set.EnabledGamePatches.Contains(patch.Name);
                    }

                    string desc = "GamePatchesSA2." + patch.Name + "Desc";
                    patch.InternalName = patch.Name;
                    patch.Name = Lang.GetString("GamePatchesSA2." + patch.Name);
                    patch.Description = Lang.GetString(desc); //need to use a variable otherwise it fails for some reason

                }

                return listPatch;
            }

            return null;
        }

        public void SetPatches()
        {
            listPatches.Items.Clear();

            List<PatchesData> patches = GetPatches(ref listPatches, GameProfile);

            if (patches is not null)
            {
                foreach (var patch in patches)
                {
                    listPatches.Items.Add(patch);
                }
            }
        }

        private void btnSelectAllPatch_Click(object sender, RoutedEventArgs e)
        {
            foreach (PatchesData patch in listPatches.Items)
            {
                patch.IsChecked = true;
            }
            RefreshPatchesList();
        }

        private void btnDeselectAllPatch_Click(object sender, RoutedEventArgs e)
        {
            foreach (PatchesData patch in listPatches.Items)
            {
                patch.IsChecked = false;
            }
            RefreshPatchesList();

        }

        private void RefreshPatchesList()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(listPatches.Items);
            view.Refresh();
        }
        #endregion

        public void SavePatches(ref object input)
        {
            GameSettings settings = input as GameSettings;

            if (listPatches is null)
                return;

            settings.EnabledGamePatches.Clear();
            foreach (PatchesData patch in listPatches.Items)
                if (patch.IsChecked == true)
                    settings.EnabledGamePatches.Add(patch.InternalName);
        }

        #region Private Functions
        private void SetupBindings()
        {
            // Graphics Bindings

            // Display Options
            comboScreen.SetBinding(ComboBox.SelectedIndexProperty, new Binding("SelectedScreen")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay,
            });
			comboScreen.ItemsSource = GraphicsManager.Screens;
			comboScreen.DisplayMemberPath = "Key";
            txtResX.MinValue = 0;
            txtResY.MinValue = 0;
            txtResX.SetBinding(NumberBox.ValueProperty, new Binding("HorizontalResolution")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
            txtResY.SetBinding(NumberBox.ValueProperty, new Binding("VerticalResolution")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
            chkRatio.SetBinding(CheckBox.IsCheckedProperty, new Binding("Enable43ResolutionRatio")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
            comboScreenMode.SetBinding(ComboBox.SelectedIndexProperty, new Binding("ScreenMode")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay,
            });
            txtCustomResX.MinValue = 0;
            txtCustomResY.MinValue = 0;
            txtCustomResX.SetBinding(NumberBox.ValueProperty, new Binding("CustomWindowWidth")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
            txtCustomResX.SetBinding(NumberBox.IsEnabledProperty, new Binding("ScreenMode")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay,
                Converter = new SA2CustomWindowEnabledConverter()
            });
            txtCustomResY.SetBinding(NumberBox.ValueProperty, new Binding("CustomWindowHeight")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
            txtCustomResY.SetBinding(NumberBox.IsEnabledProperty, new Binding("ScreenMode")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay,
                Converter = new SA2CustomWindowEnabledConverter()
            });
            comboCustomWindow.SetBinding(ComboBox.IsEnabledProperty, new Binding("ScreenMode")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay,
                Converter = new SA2CustomWindowEnabledConverter()
            });
            chkMaintainRatio.SetBinding(CheckBox.IsEnabledProperty, new Binding("ScreenMode")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay,
                Converter = new SA2CustomWindowEnabledConverter()
            });
            chkMaintainRatio.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableKeepResolutionRatio")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
            refreshRateNum.SetBinding(NumberBox.ValueProperty, new Binding("RefreshRate")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });


            // Settings
            chkPause.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnablePauseOnInactive")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
            chkResizableWin.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableResizableWindow")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
			chkStretchToWindow.SetBinding(CheckBox.IsCheckedProperty, new Binding("StretchToWindow")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay,
			});
			chkDisableBorderImage.SetBinding(CheckBox.IsCheckedProperty, new Binding("DisableBorderImage")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
            ChkSkipIntro.SetBinding(CheckBox.IsCheckedProperty, new Binding("SkipIntro")
            {
                Source = GameProfile.Graphics,
                Mode = BindingMode.TwoWay
            });
			tsTextLanguage.SetBinding(ComboBox.SelectedIndexProperty, new Binding("GameTextLanguage")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			tsVoiceLanguage.SetBinding(ComboBox.SelectedIndexProperty, new Binding("GameVoiceLanguage")
			{
				Source = GameProfile.TestSpawn,
				Mode = BindingMode.TwoWay
			});

			DebugConfig.SetBinding(DebugOptions.SettingsProperty, new Binding("DebugSettings")
			{
				Source = GameProfile,
				Mode = BindingMode.TwoWay
			});
		}
		#endregion

		private void DownloadDXVK_Click(object sender, RoutedEventArgs e)
		{
			var ps = new ProcessStartInfo("https://github.com/doitsujin/dxvk/releases")
			{
				UseShellExecute = true,
				Verb = "open"
			};
			Process.Start(ps);
		}
	}
}
