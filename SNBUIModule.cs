using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Modding;
using Modding.Serialization;
using Modding.Modules;
using Modding.Blocks;
using Modding.Common;
using Vector3 = UnityEngine.Vector3;
using USlider = UnityEngine.UI.Slider;
using System.Collections;
using System.ComponentModel;

namespace StusNavalSpace
{
    [XmlRoot("SNBUIModule")]
    [Reloadable]
    public class SNBUIModule : BlockModule
    {
        [XmlElement("HPSlider")]
        [RequireToValidate]
        public MSliderReference HitPointSlider;

        [XmlElement("EmulateKey")]
        [RequireToValidate]
        public MKeyReference EmulateKey;

        [XmlElement("ActivateKey")] //謎に必要なやつ　ブロック側は空欄で可
        [RequireToValidate]
        public MKeyReference ActivateKey;

        [XmlArray("Sounds")]
        [RequireToValidate]
        [DefaultValue(null)]
        [CanBeEmpty]
        [XmlArrayItem("AudioClip", typeof(ResourceReference))]
        public object[] Sounds;

    }

    public class SNBUIBehaviour : BlockModuleBehaviour<SNBUIModule>
    {
        public Font Orbitron;   //AssetBundleに追加したフォント、Orbiton Medium
        public Font Arial;  //Unityのデフォルトのフォント
        public GameObject ExplodePrefab;
        public GameObject ExplodeObject;
        public ParticleSystem particlesystem;

        public AudioSource audiosource;
        public AudioClip sound;

        public Color32 UseColor = new Color32(0, 255, 200, 200);
        public static Color32 HalfColor = new Color32(250, 200, 75, 200);
        public static Color32 QuarterColor = new Color32(255, 100, 100, 200);
        public static Color32 BackgroundColor = new Color32(230, 230, 230, 70);

        public MSlider HitPointSlider;  //"HPSlider"ではない
        public MKey Key;
        public MKey[] activateKeys = new MKey[1];
        public MKey activateKey;

        private int MaxHP;
        private int HalfHP;
        private int QuarterHP;

        private int BlockPlayerID;
        private int OwnerID;

        private List<Player> players;
        private Color PlayerColor;

        public Dictionary<MPTeam, Color32> ColorDict = new Dictionary<MPTeam, Color32>
        {
            { MPTeam.None, new Color32(200,200,200,225)},
            { MPTeam.Red, new Color32(255,40,0,200)},
            { MPTeam.Green, new Color32(100,255,0,200)},
            { MPTeam.Orange, new Color32(255,200,0,200)},
            { MPTeam.Blue, new Color32(0,200,255,200)},
        };

        private GameObject SNBUI;
        private GameObject SNBsubUI;

        private GameObject SNB_HPcoreUI;
        private GameObject SNB_subHPUI; //フィールドで定義しているので複数あっても大丈夫
        private Text HPText;
        private Text NameText;
        private USlider Slider;
        private string nametext;
        private bool isOwnerSame = false;

        public Joint joint;
        private bool Destroyed = false;

        private int currentHP = 1;
        private int previousHP = 1;
        private int checkFixedUpdate = 0;

        private bool ColorChange1 = false;
        private bool ColorChange2 = false;


        public override void SafeAwake()    //ブロックの設置時・シミュ開始時？（マシンにはbuilding machine とsimulation machineの２つがある）
        {
            base.SafeAwake();

            //フォントを読み込む
            Orbitron = Mod.modAssetBundle.LoadAsset<Font>("Orbitron Medium");
            Arial = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;


            //サウンドを取得
            audiosource = gameObject.AddComponent<AudioSource>();
            audiosource.spatialBlend = 1.0f;
            sound = ModResource.GetAudioClip("Hpcore");

        }

        public override void OnSimulateStart()  //シミュ開始時
        {

            //爆発エフェクトを取得・子オブジェクトとして初期化
            ExplodePrefab = Mod.modAssetBundle.LoadAsset<GameObject>("HPexplosion");
            ExplodeObject = (GameObject)Instantiate(ExplodePrefab, transform);
            particlesystem = ExplodeObject.GetComponent<ParticleSystem>();
            particlesystem.Stop();
            ExplodeObject.transform.position = BlockBehaviour.GetCenter();
            
            //HPcoreの接続を取得
            joint = GetComponent<Joint>();

            //BlockPlayerIDとPlayerIDが同一か確認
            UpdateOwnerFlag();

            //マルチ⇒プレイヤーID及びマシンの名前を取得、ソロ⇒IDを0に
            if (StatMaster.isMP)
            {
                BlockPlayerID = BlockBehaviour.ParentMachine.PlayerID;
                OwnerID = PlayerMachine.GetLocal().Player.NetworkId;
            }
            else
            {
                BlockPlayerID = 0;
            }

            base.OnSimulateStart();

            //キーを取得
            Key = GetKey(Module.EmulateKey);
            activateKey = GetKey(Module.ActivateKey);
            activateKeys[0] = activateKey;

            //HPを取得
            HitPointSlider = GetSlider(Module.HitPointSlider);
            MaxHP = (int)HitPointSlider.Value;
            HalfHP = MaxHP / 2;
            QuarterHP = MaxHP / 4;

            //HPを辞書に代入
            Mod.HPDict[BlockPlayerID] = (int)HitPointSlider.Value;

            //HPを送るタイミングをBlockPlayerID分ずらす
            checkFixedUpdate += BlockPlayerID;

            if (StatMaster.isMP)
            {
                //サーバー内のPlayerをすべて取得しリスト化
                players = Player.GetAllPlayers();

                foreach (Player child in players)
                {
                    if (child.InternalObject.networkId == (ushort)BlockPlayerID)
                    {
                        nametext = child.InternalObject.name;
                        PlayerColor = ColorDict[child.InternalObject.team];
                    }
                }

            }


            //BlockPlayerIDとPlayerIDが同一かどうかで生成するUIを変更
            if (isOwnerSame)
            {
                SNBUI = Mod.SNB_UI;
                GenerateUI();
                SNB_HPcoreUI.SetActive(true);   //UIを表示
            }
            else
            {
                SNBsubUI = Mod.SNB_UI;
                GenerateSubUI();
                SNB_subHPUI.SetActive(true);
            }

            //UIの文字とスライダーの値を更新
            HPText.text = Mod.HPDict[BlockPlayerID].ToString("D5");
            Slider.value = (float)Mod.HPDict[BlockPlayerID] / (float)MaxHP;
        }

        public override void OnSimulateStop()   //シミュ停止時
        {
            base.OnSimulateStop();

            //UIを消去
            if (isOwnerSame)
            {
                Destroy(SNB_HPcoreUI);
            }
            else
            {
                Destroy(SNB_subHPUI);
            }
        }

        public override void SimulateFixedUpdateHost()   //ホスト側のブロックだけの動作：HPが0になったらコアを破壊 / HPが更新されたらクライアントにHPを送信
        {
            base.SimulateFixedUpdateHost();

            currentHP = Mod.HPDict[BlockPlayerID];

            //HPが0以下の時の処理
            if (currentHP <= 0)
            {
                currentHP = 0;
                Mod.HPDict[BlockPlayerID] = 0;

                //HPが0以下の時は接続を破壊他色々
                if (!Destroyed)
                {
                    StartCoroutine(PlayEffect());
                    EmulateKeys(activateKeys, Key, true);
                    Destroyed = true;
                }
            }

            //HPが変わっていればUIを更新
            if (previousHP != currentHP)
            {
                //HPを送信
                Mod.message1 = Mod.messageType1.CreateMessage(BlockPlayerID, currentHP);
                ModNetworking.SendToAll(Mod.message1);

                //初めてHPが半分以下・4分の1以下になった時、UIの色を変更して再生成
                if (currentHP < HalfHP && QuarterHP < currentHP && !ColorChange1)
                {
                    UseColor = HalfColor;

                    if (isOwnerSame)
                    {
                        Destroy(SNB_HPcoreUI);
                        GenerateUI();
                    }
                    else
                    {
                        Destroy(SNB_subHPUI);
                        GenerateSubUI();
                    }

                    ColorChange1 = true;
                }
                else if (currentHP < QuarterHP && !ColorChange2)
                {
                    UseColor = QuarterColor;

                    if (isOwnerSame)
                    {
                        Destroy(SNB_HPcoreUI);
                        GenerateUI();
                    }
                    else
                    {
                        Destroy(SNB_subHPUI);
                        GenerateSubUI();
                    }

                    ColorChange2 = true;
                }

                HPText.text = currentHP.ToString("D5");
                Slider.value = (float)Mod.HPDict[BlockPlayerID] / (float)MaxHP;

                checkFixedUpdate = 0;
            }

            previousHP = currentHP;

            //50回ごとにHPの送信とUIの更新を行う
            if (checkFixedUpdate < 50)
            {
                checkFixedUpdate++;
            }
            else
            {
                //HPが0未満なら0に修正
                if (currentHP < 0)
                {
                    currentHP = 0;
                    Mod.HPDict[BlockPlayerID] = 0;
                }

                //HPを送信
                Mod.message1 = Mod.messageType1.CreateMessage(BlockPlayerID, currentHP);
                ModNetworking.SendToAll(Mod.message1);

                //初めてHPが半分以下・4分の1以下になった時、UIの色を変更して再生成
                if (currentHP < HalfHP && QuarterHP < currentHP && !ColorChange1)
                {
                    UseColor = HalfColor;

                    if (isOwnerSame)
                    {
                        Destroy(SNB_HPcoreUI);
                        GenerateUI();
                    }
                    else
                    {
                        Destroy(SNB_subHPUI);
                        GenerateSubUI();
                    }

                    ColorChange1 = true;
                }
                else if (currentHP < QuarterHP && !ColorChange2)
                {
                    UseColor = QuarterColor;

                    if (isOwnerSame)
                    {
                        Destroy(SNB_HPcoreUI);
                        GenerateUI();
                    }
                    else
                    {
                        Destroy(SNB_subHPUI);
                        GenerateSubUI();
                    }

                    ColorChange2 = true;
                }

                HPText.text = Mod.HPDict[BlockPlayerID].ToString("D5");
                Slider.value = (float)Mod.HPDict[BlockPlayerID] / (float)MaxHP;

                checkFixedUpdate = 0;
            }

        }
        public override void SimulateUpdateAlways()    //SimulateFixedUpdateClientがなぜか呼ばれなかったのでこちらで代用
        {
            base.SimulateUpdateAlways();

            //クライアントの場合の処理
            if (StatMaster.isClient)
            {
                currentHP = Mod.HPDict[BlockPlayerID];

                //初めてHPが1/2を切った時
                if (currentHP < HalfHP && QuarterHP < currentHP && !ColorChange1)
                {
                    UseColor = HalfColor;

                    if (isOwnerSame)
                    {
                        Destroy(SNB_HPcoreUI);
                        GenerateUI();
                    }
                    else
                    {
                        Destroy(SNB_subHPUI);
                        GenerateSubUI();
                    }

                    ColorChange1 = true;
                }

                //初めてHPが1/4を切った時
                else if (currentHP < QuarterHP && !ColorChange2)
                {
                    UseColor = QuarterColor;

                    if (isOwnerSame)
                    {
                        Destroy(SNB_HPcoreUI);
                        GenerateUI();
                    }
                    else
                    {
                        Destroy(SNB_subHPUI);
                        GenerateSubUI();
                    }

                    ColorChange2 = true;
                }

                //文字を更新
                HPText.text = currentHP.ToString("D5");
                Slider.value = (float)Mod.HPDict[BlockPlayerID] / (float)MaxHP;

                if (currentHP <= 0)
                {
                    //HPが0以下の時はエフェクト・サウンドを再生＆キーをエミュレート
                    if (!Destroyed)
                    {
                        StartCoroutine(PlayEffect());
                        EmulateKeys(activateKeys, Key, true);

                        Destroyed = true;
                    }
                }
            }

        }

        private void UpdateOwnerFlag()  //プレイヤーのIDとブロックの親のIDを比べる関数
        {
            if (StatMaster.isMP)
            {
                ushort BlockPlayerID = BlockBehaviour.ParentMachine.PlayerID;
                ushort LocalPlayerID = PlayerMachine.GetLocal().Player.NetworkId;
                isOwnerSame = BlockPlayerID == LocalPlayerID;
            }
            else
            {
                isOwnerSame = true;
            }
        }

        private void GenerateUI()   //自機のHP用のUIを表示する関数
        {
            SNB_HPcoreUI = new GameObject("SNB_HPcoreUI");  //HPcoreで表示させるUI全体の親
            SNB_HPcoreUI.transform.SetParent(SNBUI.transform);
            SNB_HPcoreUI.layer = LayerMask.NameToLayer("HUD");
            RectTransform SNB_HPcoreUITrans = SNB_HPcoreUI.AddComponent<RectTransform>();
            SNB_HPcoreUITrans.sizeDelta = new Vector2(300, 300);
            SNB_HPcoreUITrans.anchorMin = new Vector2(0.07f, 0.1f);
            SNB_HPcoreUITrans.anchorMax = new Vector2(0.08f, 0.1f);
            SNB_HPcoreUITrans.pivot = new Vector2(0.5f, 0.5f);
            SNB_HPcoreUITrans.anchoredPosition = new Vector2(0, 0);

            GameObject HPnumber = new GameObject("HPnumber");   //HPを数字で表示
            HPnumber.transform.SetParent(SNB_HPcoreUI.transform);
            HPnumber.layer = LayerMask.NameToLayer("HUD");
            RectTransform HPnumberRect = HPnumber.AddComponent<RectTransform>();
            HPnumberRect.anchorMin = new Vector2(0.5f, 0.5f);
            HPnumberRect.anchorMax = new Vector2(0.5f, 0.5f);
            HPnumberRect.pivot = new Vector2(0.5f, 0.5f);
            HPnumberRect.sizeDelta = new Vector2(700, 200);
            HPnumberRect.anchoredPosition = new Vector2(0, -5);
            HPnumberRect.localScale = new Vector3(0.6f, 0.6f, 1);
            HPText = HPnumber.AddComponent<Text>();
            HPText.text = "001";
            HPText.font = Orbitron;
            HPText.fontSize = 135;
            HPText.fontStyle = FontStyle.Normal;
            HPText.color = UseColor;
            HPText.alignment = TextAnchor.MiddleRight;

            //HPバーを作成
            GameObject HPSlider = new GameObject("HPbar");  //GameObjectの探索の練習用にずらしてみる
            HPSlider.transform.SetParent(SNB_HPcoreUI.transform);
            HPSlider.layer = LayerMask.NameToLayer("HUD");
            RectTransform reloadSliderRect = HPSlider.AddComponent<RectTransform>();
            reloadSliderRect.anchoredPosition = new Vector2(55, -55);
            reloadSliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            reloadSliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            reloadSliderRect.pivot = new Vector2(0.5f, 0.5f);
            reloadSliderRect.sizeDelta = new Vector2(300, 45);
            reloadSliderRect.localScale = new Vector3(1, 1, 1);
            Slider = HPSlider.AddComponent<USlider>();  //SliderにはBackGround,FillArea,Fillが必要
            GameObject background = new GameObject("Background");
            background.transform.SetParent(HPSlider.transform);
            RectTransform backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.offsetMin = new Vector2(-3.5f, -3.5f);
            backgroundRect.offsetMax = new Vector2(3.5f, 3.5f);
            backgroundRect.anchorMin = new Vector2(0, 0.25f);
            backgroundRect.anchorMax = new Vector2(1, 0.75f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            background.AddComponent<Image>().color = BackgroundColor;
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(HPSlider.transform);
            fillArea.layer = LayerMask.NameToLayer("HUD");
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.offsetMin = new Vector2(0, 0);
            fillAreaRect.offsetMax = new Vector2(0, 0);
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            GameObject fill = new GameObject("Fill");   //fillAreaの子オブジェクト
            fill.transform.SetParent(fillArea.transform);
            fill.layer = LayerMask.NameToLayer("HUD");
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.offsetMin = new Vector2(0, 0);
            fillRect.offsetMax = new Vector2(0, 0);
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fill.AddComponent<Image>().color = UseColor;

            Slider.fillRect = fillRect;
            Slider.value = (float)Mod.HPDict[BlockPlayerID] / (float)MaxHP;
        }

        private void GenerateSubUI()    //自機以外のHP用のUIを表示する関数
        {
            SNB_subHPUI = new GameObject("SNB_subHPUI");  //HPcoreで表示させるUI全体の親
            SNB_subHPUI.transform.SetParent(SNBsubUI.transform);
            SNB_subHPUI.layer = LayerMask.NameToLayer("HUD");
            RectTransform SNB_subHPUITrans = SNB_subHPUI.AddComponent<RectTransform>();
            SNB_subHPUITrans.sizeDelta = new Vector2(300, 300);
            SNB_subHPUITrans.anchorMin = new Vector2(0.07f, 0.1f);
            SNB_subHPUITrans.anchorMax = new Vector2(0.08f, 0.1f);
            SNB_subHPUITrans.pivot = new Vector2(0.5f, 0.5f);

            //自機の分が抜けることに注意しつつ表示場所を調整する
            if (BlockPlayerID > OwnerID)
            {
                SNB_subHPUITrans.anchoredPosition = new Vector2(0, 450 + 75 * (BlockPlayerID - 1));  //自機のIDより上は１マス下にずらす
            }
            else
            {
                SNB_subHPUITrans.anchoredPosition = new Vector2(0, 450 + 75 * BlockPlayerID);
            }

            SNB_subHPUITrans.localScale = new Vector3(0.6f, 0.6f, 1);

            //HPを数字で表示
            GameObject HPnumber = new GameObject("HPnumber");
            HPnumber.transform.SetParent(SNB_subHPUI.transform);
            HPnumber.layer = LayerMask.NameToLayer("HUD");
            RectTransform HPnumberRect = HPnumber.AddComponent<RectTransform>();
            HPnumberRect.anchorMin = new Vector2(0.5f, 0.5f);
            HPnumberRect.anchorMax = new Vector2(0.5f, 0.5f);
            HPnumberRect.pivot = new Vector2(0.5f, 0.5f);
            HPnumberRect.sizeDelta = new Vector2(500, 200);
            HPnumberRect.anchoredPosition = new Vector2(25, -25);
            HPnumberRect.localScale = new Vector3(0.6f, 0.6f, 1);
            HPText = HPnumber.AddComponent<Text>();
            HPText.text = "001";
            HPText.font = Orbitron;
            HPText.fontSize = 55;
            HPText.fontStyle = FontStyle.Normal;
            HPText.color = UseColor;
            HPText.alignment = TextAnchor.MiddleRight;

            //HPバーを作成
            GameObject HPSlider = new GameObject("HPbar");  //GameObjectの探索の練習用にずらしてみる
            HPSlider.transform.SetParent(SNB_subHPUI.transform);
            HPSlider.layer = LayerMask.NameToLayer("HUD");
            RectTransform reloadSliderRect = HPSlider.AddComponent<RectTransform>();
            reloadSliderRect.anchoredPosition = new Vector2(-15, -55);
            reloadSliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            reloadSliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            reloadSliderRect.pivot = new Vector2(0.5f, 0.5f);
            reloadSliderRect.sizeDelta = new Vector2(235, 16);
            reloadSliderRect.localScale = new Vector3(1, 1, 1);

            Slider = HPSlider.AddComponent<USlider>();  //SliderにはBackGround,FillArea,Fillが必要
            GameObject background = new GameObject("Background");
            background.transform.SetParent(HPSlider.transform);
            RectTransform backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.offsetMin = new Vector2(-3.5f, -3.5f);
            backgroundRect.offsetMax = new Vector2(3.5f, 3.5f);
            backgroundRect.anchorMin = new Vector2(0, 0.25f);
            backgroundRect.anchorMax = new Vector2(1, 0.75f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            background.AddComponent<Image>().color = BackgroundColor;
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(HPSlider.transform);
            fillArea.layer = LayerMask.NameToLayer("HUD");
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.offsetMin = new Vector2(0, 0);
            fillAreaRect.offsetMax = new Vector2(0, 0);
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            GameObject fill = new GameObject("Fill");   //fillAreaの子オブジェクト
            fill.transform.SetParent(fillArea.transform);
            fill.layer = LayerMask.NameToLayer("HUD");
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.offsetMin = new Vector2(0, 0);
            fillRect.offsetMax = new Vector2(0, 0);
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fill.AddComponent<Image>().color = UseColor;

            Slider.fillRect = fillRect;
            Slider.value = (float)Mod.HPDict[BlockPlayerID] / (float)MaxHP;

            //プレイヤー名を表示
            GameObject Name = new GameObject("Name");
            Name.transform.SetParent(SNB_subHPUI.transform);
            Name.layer = LayerMask.NameToLayer("HUD");
            RectTransform NameRect = Name.AddComponent<RectTransform>();
            NameRect.anchorMin = new Vector2(0.5f, 0.5f);
            NameRect.anchorMax = new Vector2(0.5f, 0.5f);
            NameRect.pivot = new Vector2(0.5f, 0.5f);
            NameRect.sizeDelta = new Vector2(700, 200);
            NameRect.anchoredPosition = new Vector2(-10, 5);
            NameRect.localScale = new Vector3(0.6f, 0.6f, 1);
            NameText = Name.AddComponent<Text>();
            NameText.text = nametext;
            NameText.font = Arial;
            NameText.fontSize = 75;
            NameText.fontStyle = FontStyle.Normal;
            NameText.color = PlayerColor;
            NameText.alignment = TextAnchor.MiddleLeft;
        }

        //何かしらのエミュレートがある時に必要なおまじない
        public override bool EmulatesAnyKeys
        {
            get { return true; }
        }

        //エフェクトと音を出す関数
        public IEnumerator PlayEffect()
        {
            //１秒待つ
            yield return new WaitForSeconds(1f);

            //エフェクトとサウンドを再生
            particlesystem.Play();
            audiosource.PlayOneShot(sound);

            //接続を消去
            if (StatMaster.isHosting || !StatMaster.isMP || StatMaster.isLocalSim)  //ホストorマルチでないorローカルシミュ
            {
                Destroy(joint);
            }

            yield return new WaitForSeconds(2f);
            particlesystem.Stop();
        }
    }

}
