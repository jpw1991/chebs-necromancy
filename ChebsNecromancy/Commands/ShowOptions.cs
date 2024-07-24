using ChebsNecromancy.Items.Armor.Player;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Skeletons;
using ChebsValheimLibrary.PvP;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Commands;

public class ShowOptions : ConsoleCommand
{
    private static GameObject _panel;
    private static Dropdown _boneColorDropdown, _eyeColorDropdown, _emblemDropdown;
    private static Text _alliesText;
    private static InputField _allyInput;
    
    public override string Name => "chebgonaz_options";

    public override string Help => "Shows mod options.";

    public override void Run(string[] args)
    {
        TogglePanel();
    }
    private void TogglePanel()
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
                _eyeColorDropdown.onValueChanged.AddListener(delegate
                {
                    var newColor = (UndeadMinion.EyeColor)_eyeColorDropdown.value;
                    UndeadMinion.SetEyeColor(newColor);
                });
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
                _boneColorDropdown.onValueChanged.AddListener(delegate
                {
                    var newColor = (SkeletonMinion.BoneColor)_boneColorDropdown.value;
                    SkeletonMinion.SetBoneColor(newColor);
                });
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
            }

            {
                // PvP stuff
                // var grid = new GameObject();
                // var rectTransform = grid.AddComponent<RectTransform>();
                // grid.transform.SetParent(_panel.transform);
                // rectTransform.anchoredPosition = Vector2.zero;
                // rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
                // var verticalLayoutGroup = grid.AddComponent<VerticalLayoutGroup>();
                // var contentSizeFitter = grid.AddComponent<ContentSizeFitter>();
                // contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                // contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                //
                // var playerList = Player.GetAllPlayers().Select(p => p.GetPlayerName());
                // var allies = PvPManager.GetPlayerFriends();
                // foreach (var player in playerList)
                // {
                //     var allied = allies.Contains(player);
                //     AddRow(grid.transform, player, allied);
                // }
                
                // list
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
            var buttonObject = GUIManager.Instance.CreateButton("Close", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, -250f),
                width: 250f, height: 60f);
            buttonObject.SetActive(true);
            buttonObject.GetComponent<Button>().onClick.AddListener(TogglePanel);
        }
        
        var state = !_panel.activeSelf;
        _panel.SetActive(state);

        if (state)
        {
            var friends = PvPManager.GetPlayerFriends();
            _alliesText.text = string.Join(", ", friends);

            var player = Player.m_localPlayer;
            var eyeColor = player.m_nview.GetZDO().GetInt(UndeadMinion.PlayerEyeColorZdoKeyHash);
            var boneColor = player.m_nview.GetZDO().GetInt(SkeletonMinion.PlayerBoneColorZdoKeyHash);
            _boneColorDropdown.SetValueWithoutNotify(boneColor);
            _eyeColorDropdown.SetValueWithoutNotify(eyeColor);
        }
        
        // Toggle input for the player and camera while displaying the GUI
        GUIManager.BlockInput(state);
    }
    
    // void AddRow(Transform parent, string text, bool isChecked)
    // {
    //     // Create a row
    //     var row = new GameObject("Row");
    //     var rowRectTransform = row.AddComponent<RectTransform>();
    //     row.transform.SetParent(parent);
    //     var horizontalLayoutGroup = row.AddComponent<HorizontalLayoutGroup>();
    //     var rowContentSizeFitter = row.AddComponent<ContentSizeFitter>();
    //     rowContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    //     rowContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    //
    //     // Create and add the text column
    //     var textColumn = new GameObject("TextColumn");
    //     var textRectTransform = textColumn.AddComponent<RectTransform>();
    //     textColumn.transform.SetParent(row.transform);
    //     var textComponent = textColumn.AddComponent<Text>();
    //     textComponent.text = text;
    //     textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // Use built-in Arial font
    //     textComponent.color = Color.black; // Set text color
    //
    //     // Create and add the checkbox column
    //     var checkboxColumn = new GameObject("CheckboxColumn");
    //     var checkboxRectTransform = checkboxColumn.AddComponent<RectTransform>();
    //     checkboxColumn.transform.SetParent(row.transform);
    //     var toggleComponent = checkboxColumn.AddComponent<Toggle>();
    //
    //     // Create Background for Toggle
    //     var background = new GameObject("Background");
    //     background.transform.SetParent(checkboxColumn.transform);
    //     var backgroundRectTransform = background.AddComponent<RectTransform>();
    //     backgroundRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
    //     backgroundRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    //     backgroundRectTransform.sizeDelta = new Vector2(20, 20);
    //     var backgroundImage = background.AddComponent<Image>();
    //     backgroundImage.color = Color.white;
    //     toggleComponent.targetGraphic = backgroundImage;
    //
    //     // Create Checkmark for Toggle
    //     var checkmark = new GameObject("Checkmark");
    //     checkmark.transform.SetParent(background.transform);
    //     var checkmarkRectTransform = checkmark.AddComponent<RectTransform>();
    //     checkmarkRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
    //     checkmarkRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    //     checkmarkRectTransform.sizeDelta = new Vector2(20, 20);
    //     var checkmarkImage = checkmark.AddComponent<Image>();
    //     checkmarkImage.color = Color.black;
    //     toggleComponent.graphic = checkmarkImage;
    //
    //     toggleComponent.isOn = isChecked;
    // }
}