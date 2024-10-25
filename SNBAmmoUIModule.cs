using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Modding;
using Modding.Serialization;
using Modding.Modules;
using Modding.Blocks;
using skpCustomModule;
using Vector3 = UnityEngine.Vector3;
using USlider = UnityEngine.UI.Slider;

namespace StusNavalSpace
{
    [XmlRoot("SNBAmmoUIModule")]
    [Reloadable]
    public class SNBAmmoUIModule : BlockModule
    {
        [XmlElement("UseAmmoUIToggle")]
        [RequireToValidate]
        public MToggleReference useAmmoUIToggle;

        [XmlElement("OffsetUISlider")]
        [RequireToValidate]
        public MSliderReference offsetUISlider;

        [XmlElement("RateOfFireSlider")]
        [RequireToValidate]
        public MSliderReference rateOfFireSlider;

        [XmlElement("useMagazine")]
        [DefaultValue(false)]
        [Reloadable]
        public bool useMagazine;

        [XmlElement("ReloadTimeSlider")]
        [RequireToValidate]
        [DefaultValue(null)]
        public MSliderReference reloadTimeSlider;

        [XmlElement("Icon")]
        [RequireToValidate]
        public ResourceReference icon;

    }
    public class SNBAmmoUIBehaviour : BlockModuleBehaviour<SNBAmmoUIModule>
    {
        private AdShootingBehavour shootingBehavour;
        public Sprite AmmoIconImage;
        public static Font font;    //font:Orbitron Medium
        public Color32 BackgroundColor = new Color32(60, 60, 60, 200);
        public Color32 UIColor = new Color32(0, 255, 200, 200);
        public Color32 AlertColor = new Color32(60, 60, 60, 200);
        public Color32 MagazineReloadColor = new Color32(255, 200, 0, 200);
        public Color32 UseColor = new Color32(0, 255, 200, 200);
        public MToggle useAmmoUIToggle;
        public MSlider offsetUISlider;
        public MSlider rateOfFireSlider;
        public MSlider reloadTimeSlider;
        private GameObject ammoUI;
        private GameObject SNBUI;
        private Text magazineText;
        private Text totalText;
        private float fireRate = 0.1f;
        private bool isOwnerSame = false;
        private bool MagazineColorOn = false;

        private bool init = false;

        public bool Reloading = false;

        public override void SafeAwake()
        {
            base.SafeAwake();

            font = Mod.modAssetBundle.LoadAsset<Font>("Orbitron Medium");
            shootingBehavour = base.GetComponent<AdShootingBehavour>();
            shootingBehavour.OnFire += OnFire;  //射撃時に呼ぶ関数に自身のOnFire()を追加

            UpdateOwnerFlag();

            try
            {
                useAmmoUIToggle = GetToggle(Module.useAmmoUIToggle);
                offsetUISlider = GetSlider(Module.offsetUISlider);
                offsetUISlider.ValueChanged += offsetUIValueChanged;
                rateOfFireSlider = GetSlider(Module.rateOfFireSlider);
                ModTexture modTexture = (ModTexture)GetResource(Module.icon);
                modTexture.Texture.wrapMode = TextureWrapMode.Clamp;
                AmmoIconImage = Sprite.Create(modTexture.Texture, new Rect(0, 0, modTexture.Texture.width, modTexture.Texture.height), Vector2.zero);

                if (Module.useMagazine)
                {
                    reloadTimeSlider = GetSlider(Module.reloadTimeSlider);
                }
            }
            catch
            {
                Debug.Log("Error BlockName : " + name);
                Destroy(this);
                return;
            }
        }

        public override void OnSimulateStart()  //シミュ開始時にUIを作成
        {
            base.OnSimulateStart(); //OnSimulateStart：ホストクライアント両方で実行される

            UpdateOwnerFlag();  //自身に対してのみisOwnerSameをtrueにする
            if (!isOwnerSame) return;

            SNBUI = Mod.SNB_UI;
            GenerateUI();
            fireRate = rateOfFireSlider.Value;

            ammoUI.SetActive(false);
        }

        public override void OnSimulateStop()   //シミュ停止時にUIを削除
        {
            base.OnSimulateStop(); //OnSimulateStop：ホストクライアント両方で実行される
            if (!isOwnerSame) return;
            Destroy(ammoUI);

            init = false;
        }

        public override void SimulateUpdateAlways() //シミュ中の挙動
        {
            base.SimulateUpdateAlways(); //SimulateUpdateAlways：シミュ中のすべての物体で実行？
            if (!isOwnerSame) return;   //自身以外には表示しない

            //UIを表示しないなら飛ばす
            if(!useAmmoUIToggle.IsActive)
            {
                return;
            }

            //初回のみUIを表示
            if (!init)
            {
                ammoUI.SetActive(true); //UIを表示
                init = true;
            }

            if (base.Machine.InfiniteAmmo || !Module.useMagazine)
            {
                totalText.text = "-----";
            }
            else
            {
                totalText.text = IntToThreeDigitString(shootingBehavour.AmmoStock);
            }

            magazineText.text = IntToThreeDigitString(shootingBehavour.AmmoLeft);

        }

        private void OnFire()
        {

            if (!isOwnerSame) return;
            if (!StatMaster.levelSimulating) return;
            if (!useAmmoUIToggle.IsActive) return;

            if (Module.useMagazine && shootingBehavour.AmmoLeft == 0)   //マガジンリロード時
            {
                Destroy(ammoUI);
                UseColor = MagazineReloadColor;
                MagazineColorOn = true;
                GenerateUI();
                StartCoroutine(BulletTimerFadeIn(reloadTimeSlider.Value));
            }
            else if (totalText.text == "-----" && shootingBehavour.AmmoLeft == 0)   //残弾が0の時
            {
                UseColor = AlertColor;
                Destroy(ammoUI);
                GenerateUI();
                StartCoroutine(BulletTimerFadeIn(1 / fireRate));
            }
            else
            {
                StartCoroutine(BulletTimerFadeIn(1 / fireRate));
            }
        }

        IEnumerator BulletTimerFadeIn(float time)   //リロード中にリロードスライダーを操作する関数
        {
            Reloading = true;
            float rate = 1 / time;
            USlider slider = ammoUI.transform.Find("ReloadTimer").gameObject.GetComponent<USlider>();
            float timer = 0f;
            slider.value = 0f;
            while (timer <= time)
            {
                slider.value = Mathf.Lerp(0f, 1f, timer * rate);
                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            slider.value = 1f;
            if (MagazineColorOn == true)
            {
                UseColor = UIColor;
                MagazineColorOn = false;
                Destroy(ammoUI);
                GenerateUI();
            }

            Reloading = false;
        }

        public string IntToThreeDigitString(int num)    //int型を3桁に丸める関数
        {
            // 999以上の場合は999に丸める
            if (num > 999) num = 999;

            // 整数を3桁にする
            string numStr = num.ToString("D3");

            // 結果を返す
            return numStr;
        }

        public string IntToThreeDigitString(float num)  //float型を3桁に丸める関数
        {
            // 数値を整数に変換して小数点以下を切り捨てる
            int numInt = Mathf.FloorToInt(num);

            return IntToThreeDigitString(numInt);
        }

        private void offsetUIValueChanged(float value)  //offsetUIを整数に丸める関数
        {
            offsetUISlider.Value = Mathf.Round(value);
        }

        private void UpdateOwnerFlag()  //プレイヤーとブロックの親が一緒か確認する関数
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

        private void GenerateUI()
        {
            // AmmoUIオブジェクトを作成
            ammoUI = new GameObject("AmmoUI");
            ammoUI.transform.SetParent(SNBUI.transform);
            ammoUI.layer = LayerMask.NameToLayer("HUD");
            RectTransform ammoUITrans = ammoUI.AddComponent<RectTransform>();
            ammoUITrans.sizeDelta = new Vector2(300, 80);
            ammoUITrans.anchorMin = new Vector2(0, 0);
            ammoUITrans.anchorMax = new Vector2(0, 0);
            ammoUITrans.pivot = new Vector2(0.5f, 0.5f);
            ammoUITrans.anchoredPosition = new Vector2(180, 200 + (offsetUISlider.Value * 70));

            // 弾丸のアイコンを表示するためのオブジェクトを作成
            GameObject ammoIcon = new GameObject("AmmoIcon");
            ammoIcon.transform.SetParent(ammoUI.transform);
            ammoIcon.layer = LayerMask.NameToLayer("HUD");
            RectTransform iconRect = ammoIcon.AddComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(-120, 0);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(100, 100);
            iconRect.localScale = new Vector3(1, 1, 1);
            Image ammoImage = ammoIcon.AddComponent<Image>();
            ammoImage.sprite = AmmoIconImage;
            ammoImage.color = UseColor;


            // リロード時間を表示するためのスライダーを作成
            GameObject reloadSlider = new GameObject("ReloadTimer");
            reloadSlider.transform.SetParent(ammoUI.transform);
            reloadSlider.layer = LayerMask.NameToLayer("HUD");
            RectTransform reloadSliderRect = reloadSlider.AddComponent<RectTransform>();
            reloadSliderRect.anchoredPosition = new Vector2(55, -30);
            reloadSliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            reloadSliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            reloadSliderRect.pivot = new Vector2(0.5f, 0.5f);
            reloadSliderRect.sizeDelta = new Vector2(240, 16);
            reloadSliderRect.localScale = new Vector3(1, 1, 1);
            USlider slider = reloadSlider.AddComponent<USlider>();
            GameObject background = new GameObject("Background");
            background.transform.SetParent(reloadSlider.transform);
            RectTransform backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.offsetMin = new Vector2(5, 0);
            backgroundRect.offsetMax = new Vector2(-10, 0);
            backgroundRect.anchorMin = new Vector2(0, 0.25f);
            backgroundRect.anchorMax = new Vector2(1, 0.75f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            background.AddComponent<Image>().color = BackgroundColor;
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(reloadSlider.transform);
            fillArea.layer = LayerMask.NameToLayer("HUD");
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-15, 0);
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            fill.layer = LayerMask.NameToLayer("HUD");
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.offsetMin = new Vector2(-5, 0);
            fillRect.offsetMax = new Vector2(5, 0);
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fill.AddComponent<Image>().color = UseColor;
            slider.fillRect = fillRect;
            slider.value = 1f;

            GameObject count = new GameObject("Count"); //弾数表示全体の親
            count.transform.SetParent(ammoUI.transform);
            count.layer = LayerMask.NameToLayer("HUD");
            RectTransform countRect = count.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0.5f);
            countRect.anchorMax = new Vector2(0.5f, 0.5f);
            countRect.pivot = new Vector2(0.5f, 0.5f);
            countRect.sizeDelta = new Vector2(100, 100);
            countRect.anchoredPosition = new Vector2(10, -5);

            GameObject magazine = new GameObject("Magazine");
            magazine.transform.SetParent(count.transform);
            magazine.layer = LayerMask.NameToLayer("HUD");
            RectTransform magazineRect = magazine.AddComponent<RectTransform>();
            magazineRect.anchorMin = new Vector2(0.5f, 0.5f);
            magazineRect.anchorMax = new Vector2(0.5f, 0.5f);
            magazineRect.pivot = new Vector2(0.5f, 0.5f);
            magazineRect.sizeDelta = new Vector2(220, 90);
            magazineRect.anchoredPosition = new Vector2(-35, 0);
            magazineRect.localScale = new Vector3(0.7f, 0.8f, 1);
            magazineText = magazine.AddComponent<Text>();
            magazineText.text = "010";
            magazineText.font = font;
            magazineText.fontSize = 65;
            magazineText.fontStyle = FontStyle.Italic;
            magazineText.color = UseColor;
            magazineText.alignment = TextAnchor.MiddleRight;

            GameObject slash = new GameObject("Slash");
            slash.transform.SetParent(count.transform);
            slash.layer = LayerMask.NameToLayer("HUD");
            RectTransform slashRect = slash.AddComponent<RectTransform>();
            slashRect.anchorMin = new Vector2(0.5f, 0.5f);
            slashRect.anchorMax = new Vector2(0.5f, 0.5f);
            slashRect.pivot = new Vector2(0.5f, 0.5f);
            slashRect.sizeDelta = new Vector2(220, 90);
            slashRect.anchoredPosition = new Vector2(-5, 0);
            slashRect.localScale = new Vector3(0.7f, 0.8f, 1);
            Text slashText = slash.AddComponent<Text>();
            slashText.text = "/";
            slashText.font = font;
            slashText.fontSize = 65;
            slashText.fontStyle = FontStyle.Italic;
            slashText.color = UseColor;
            slashText.alignment = TextAnchor.MiddleRight;

            GameObject total = new GameObject("Total");
            total.transform.SetParent(count.transform);
            total.layer = LayerMask.NameToLayer("HUD");
            RectTransform totalRect = total.AddComponent<RectTransform>();
            totalRect.anchorMin = new Vector2(0.5f, 0.5f);
            totalRect.anchorMax = new Vector2(0.5f, 0.5f);
            totalRect.pivot = new Vector2(0.5f, 0.5f);
            totalRect.sizeDelta = new Vector2(220, 90);
            totalRect.anchoredPosition = new Vector2(95, -5);
            totalRect.localScale = new Vector3(0.5f, 0.6f, 1);
            totalText = total.AddComponent<Text>();
            totalText.text = "090";
            totalText.font = font;
            totalText.fontSize = 60;
            totalText.fontStyle = FontStyle.Italic;
            totalText.color = UseColor;
            totalText.alignment = TextAnchor.MiddleRight;
        }
    }
}