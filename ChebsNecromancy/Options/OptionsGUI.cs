using ChebsNecromancy.Items.Armor.Player;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Skeletons;
using ChebsValheimLibrary.PvP;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Options;

public class OptionsGUI
{
    private static GameObject _panel;
    private static Dropdown _boneColorDropdown, _eyeColorDropdown, _emblemDropdown;
    private static Text _alliesText;
    private static InputField _allyInput;
    
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
            
            DragWindowCntrl.ApplyDragWindowCntrl(_panel);
            
            GUIManager.Instance.CreateText("Cheb's Necromancy Options", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -50f),
                font: GUIManager.Instance.AveriaSerifBold, fontSize: 30, color: GUIManager.Instance.ValheimOrange,
                outline: true, outlineColor: Color.black,
                width: 350f, height: 40f, addContentSizeFitter: false);

            {
                // Minion eye colors
                GUIManager.Instance.CreateText("Minion Eye Color:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);
            
                var dropdownObject = GUIManager.Instance.CreateDropDown(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 210f),
                    fontSize: 16,
                    width: 200f, height: 30f);
                _eyeColorDropdown = dropdownObject.GetComponent<Dropdown>();
                _eyeColorDropdown.AddOptions(Enum.GetValues(typeof(UndeadMinion.EyeColor))
                    .Cast<UndeadMinion.EyeColor>()
                    .Select(o =>$"{o}")
                    .ToList());
                // _eyeColorDropdown.onValueChanged.AddListener(delegate
                // {
                //     var newColor = (UndeadMinion.EyeColor)_eyeColorDropdown.value;
                //     UndeadMinion.SetEyeColor(newColor);
                //     Options.EyeColor = newColor;
                // });
            }

            {
                // Skeleton bone colors
                GUIManager.Instance.CreateText("Skeleton Bone Color:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -140f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);
            
                var dropdownObject = GUIManager.Instance.CreateDropDown(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 170f),
                    fontSize: 16,
                    width: 200f, height: 30f);
                _boneColorDropdown = dropdownObject.GetComponent<Dropdown>();
                _boneColorDropdown.AddOptions(Enum.GetValues(typeof(SkeletonMinion.BoneColor))
                    .Cast<SkeletonMinion.BoneColor>()
                    .Select(o =>$"{o}")
                    .ToList());
                // _boneColorDropdown.onValueChanged.AddListener(delegate
                // {
                //     var newColor = (SkeletonMinion.BoneColor)_boneColorDropdown.value;
                //     SkeletonMinion.SetBoneColor(newColor);
                //     Options.BoneColor = newColor;
                // });
            }
            
            {
                // Minion cape emblems
                GUIManager.Instance.CreateText("Minion Cape Emblem:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -180f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);
            
                var dropdownObject = GUIManager.Instance.CreateDropDown(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 130f),
                    fontSize: 16,
                    width: 200f, height: 30f);
                _emblemDropdown = dropdownObject.GetComponent<Dropdown>();
                _emblemDropdown.AddOptions(Enum.GetValues(typeof(NecromancerCape.Emblem))
                    .Cast<NecromancerCape.Emblem>()
                    .Select(o =>$"{o}")
                    .ToList());
                // _emblemDropdown.onValueChanged.AddListener(delegate
                // {
                //     var newEmblem = (NecromancerCape.Emblem)_emblemDropdown.value;
                //     NecromancerCape.SetEmblem(newEmblem);
                //     Options.Emblem = newEmblem;
                // });
            }

            {
                // PvP stuff
                var allies = PvPManager.GetPlayerFriends();
                GUIManager.Instance.CreateText("PvP Allies:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -210f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);
                
                var textObject = GUIManager.Instance.CreateText(string.Join(", ", allies), parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -210f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 400f, height: 30f, addContentSizeFitter: false);
                _alliesText = textObject.GetComponentInChildren<Text>();
                
                // add/remove ally
                GUIManager.Instance.CreateText("Ally (case sensitive):", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -240f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);
                
                _allyInput = GUIManager.Instance.CreateInputField(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 60f),
                    contentType: InputField.ContentType.Standard,
                    placeholderText: "player",
                    fontSize: 16,
                    width: 200f,
                    height: 30f).GetComponentInChildren<InputField>();
                _allyInput.characterValidation = InputField.CharacterValidation.Alphanumeric;
                
                GUIManager.Instance.CreateButton("+", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(200f, 60f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (_allyInput.text != string.Empty)
                        {
                            var newAlly = _allyInput.text;
                            var friends = PvPManager.GetPlayerFriends();
                            if (!friends.Contains(newAlly))
                            {
                                friends.Add(newAlly);
                                PvPManager.UpdatePlayerFriendsDict(friends);
                                _alliesText.text = string.Join(", ", friends);
                            }

                            _allyInput.text = string.Empty;
                        }
                    });
                
                GUIManager.Instance.CreateButton("-", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(260f, 60f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_allyInput.text != string.Empty)
                    {
                        var newAlly = _allyInput.text;
                        var friends = PvPManager.GetPlayerFriends();
                        if (friends.Contains(newAlly))
                        {
                            friends.Remove(newAlly);
                            PvPManager.UpdatePlayerFriendsDict(friends);
                            _alliesText.text = string.Join(", ", friends);
                        }

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
                // save options
                Options.BoneColor = (SkeletonMinion.BoneColor)_boneColorDropdown.value;
                Options.EyeColor = (UndeadMinion.EyeColor)_eyeColorDropdown.value;
                Options.Emblem = (NecromancerCape.Emblem)_emblemDropdown.value;
                Options.SaveOptions();
                
                // apply options
                UndeadMinion.SetEyeColor(Options.EyeColor);
                SkeletonMinion.SetBoneColor(Options.BoneColor);
                NecromancerCape.SetEmblem(Options.Emblem);
                
                TogglePanel();
            });
        }
        
        var state = !_panel.activeSelf;
        _panel.SetActive(state);

        var friends = PvPManager.GetPlayerFriends();
        _alliesText.text = string.Join(", ", friends);
        
        _boneColorDropdown.value = (int)Options.BoneColor;
        _boneColorDropdown.RefreshShownValue();
        _eyeColorDropdown.value = (int)Options.EyeColor;
        _eyeColorDropdown.RefreshShownValue();
        _emblemDropdown.value = (int)Options.Emblem;
        _emblemDropdown.RefreshShownValue();
        
        Logger.LogInfo($"_boneColorDropdown.value={_boneColorDropdown.value} (int)Options.BoneColor={(int)Options.BoneColor}/{Options.BoneColor} " +
                       $"_eyeColorDropdown.value={_eyeColorDropdown.value} (int)Options.EyeColor={(int)Options.EyeColor}/{Options.EyeColor} " +
                       $"_emblemDropdown.value={_emblemDropdown.value} (int)Options.Emblem={(int)Options.Emblem}/{Options.Emblem}");
        
        // Toggle input for the player and camera while displaying the GUI
        GUIManager.BlockInput(state);
    }
}