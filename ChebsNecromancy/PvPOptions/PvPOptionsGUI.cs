using BepInEx;
using BepInEx.Configuration;
using ChebsValheimLibrary.PvP;
using Jotunn.Configs;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.PvPOptions;

public class PvPOptionsGUI
{
    private static GameObject _panel;
    private static Text _alliesText;
    private static InputField _allyInput;

    private static List<string> _unsavedFriends;

    public static ConfigEntry<KeyboardShortcut> OptionsKeyConfigEntry;
    public static ButtonConfig OptionsButton;

    public static void CreateConfigs(BaseUnityPlugin plugin, string pluginGuid)
    {
        const string client = "PvP Options (Client)";

        OptionsKeyConfigEntry = plugin.Config.Bind(client, "PvPOpenOptions",
            new KeyboardShortcut(KeyCode.F7), new ConfigDescription("Open the mod PvP options window."));
        
        OptionsButton = new ButtonConfig
        {
            Name = "PvPOptionsButton",
            ShortcutConfig = OptionsKeyConfigEntry,
            HintToken = "PvPOptionsButton"
        };
        InputManager.Instance.AddButton(pluginGuid, OptionsButton);
    }

    public static void TogglePanel()
    {
        // abort if player's not in game
        if (Player.m_localPlayer == null) return;

        // Create the panel if it does not exist
        if (!_panel)
        {
            if (GUIManager.Instance == null)
            {
                Logger.LogError("GUIManager instance is null");
                return;
            }

            if (!GUIManager.CustomGUIFront)
            {
                Logger.LogError("GUIManager CustomGUI is null");
                return;
            }

            _panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 850,
                height: 600,
                draggable: false);
            _panel.SetActive(false);

            _panel.AddComponent<DragWindowCntrl>();

            GUIManager.Instance.CreateText("PvP Options", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -50f),
                font: GUIManager.Instance.AveriaSerifBold, fontSize: 30, color: GUIManager.Instance.ValheimOrange,
                outline: true, outlineColor: Color.black,
                width: 350f, height: 40f, addContentSizeFitter: false);
            
            {
                // PvP stuff
                var allies = PvPManager.GetPlayerFriends();
                GUIManager.Instance.CreateText("PvP Allies:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);

                var textObject = GUIManager.Instance.CreateText(string.Join(", ", allies), parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 400f, height: 30f, addContentSizeFitter: false);
                _alliesText = textObject.GetComponentInChildren<Text>();

                // add/remove ally
                GUIManager.Instance.CreateText("Ally (case sensitive):", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -140f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);

                _allyInput = GUIManager.Instance.CreateInputField(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 170f),
                    contentType: InputField.ContentType.Standard,
                    placeholderText: "player",
                    fontSize: 16,
                    width: 200f,
                    height: 30f).GetComponentInChildren<InputField>();
                _allyInput.characterValidation = InputField.CharacterValidation.None;

                GUIManager.Instance.CreateButton("+", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(200f, 170f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_allyInput.text != string.Empty)
                    {
                        var newAlly = _allyInput.text;
                        if (!_unsavedFriends.Contains(newAlly))
                        {
                            _unsavedFriends.Add(newAlly);
                        }

                        _alliesText.text = string.Join(", ", _unsavedFriends);

                        _allyInput.text = string.Empty;
                    }
                });

                GUIManager.Instance.CreateButton("-", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(260f, 170f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_allyInput.text != string.Empty)
                    {
                        var newAlly = _allyInput.text;
                        if (_unsavedFriends.Contains(newAlly))
                        {
                            _unsavedFriends.Remove(newAlly);
                        }

                        _alliesText.text = string.Join(", ", _unsavedFriends);

                        _allyInput.text = string.Empty;
                    }
                });
            }

            // close button
            GUIManager.Instance.CreateButton("Cancel", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, -250f),
                width: 250f, height: 60f).GetComponent<Button>().onClick.AddListener(TogglePanel);

            GUIManager.Instance.CreateButton("Save", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(250f, -250f),
                width: 250f, height: 60f).GetComponent<Button>().onClick.AddListener(() =>
            {
                // save PvP
                PvPManager.UpdatePlayerFriendsDict(_unsavedFriends);

                TogglePanel();
            });
        }

        var state = !_panel.activeSelf;
        _panel.SetActive(state);

        _unsavedFriends = PvPManager.GetPlayerFriends()
            .ToList(); // ensure new copy, not byref. Fixed in CVL 2.6.3
        _alliesText.text = string.Join(", ", _unsavedFriends);

        // Toggle input for the player and camera while displaying the GUI
        GUIManager.BlockInput(state);
    }
}