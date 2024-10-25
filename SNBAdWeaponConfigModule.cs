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
using skpCustomModule;

namespace StusNavalSpace
{
    [XmlRoot("SNBAdWeaponConfigModule")]
    [Reloadable]

    public class SNBAdWeaponConfigModule : BlockModule
    {
        [XmlElement("DamageSlider")]    //XMLにはこの名前が使われる
        [RequireToValidate]
        public MSliderReference DamageSlider;

        [XmlElement("ExplodeRadius")]
        [RequireToValidate]
        public MSliderReference RadiusSlider;

        [XmlElement("MaxAmmo")]
        [RequireToValidate]
        public MSliderReference AmmoSlider;
    }

    public class SNBAdWeaponConfigBehaviour : BlockModuleBehaviour<SNBAdWeaponConfigModule>
    {
        public MSlider damageslider;
        public int Damage;
        public MSlider radiusslider;
        public int Radius;
        public MSlider ammoslider;
        public int ammo;

        private Transform effectpool;

        private AdShootingBehavour adshootingbehavour;
        private AdExplosionEffect adexplosioneffect;

        private SNBAdExplosionController snbadexplosioncontroller;


        public override void OnSimulateStart()
        {
            base.OnSimulateStart();

            adshootingbehavour = GetComponent<AdShootingBehavour>();
            Debug.Log(adshootingbehavour);
            damageslider = GetSlider(Module.DamageSlider);
            Debug.Log(damageslider);
            Damage = (int)damageslider.Value;
            
            radiusslider = GetSlider(Module.RadiusSlider);
            Debug.Log(radiusslider);
            Radius = (int)radiusslider.Value;
            
            
            //総弾数を調整
            ammoslider = GetSlider(Module.AmmoSlider);
            Debug.Log(ammoslider);
            ammo = (int)ammoslider.Value;
            

            if (adshootingbehavour.AmmoStock != 0)  //マガジンの弾数を見てマガジンを使っているか判断
            {
                adshootingbehavour.AmmoLeft = ammo - adshootingbehavour.AmmoStock;  //総弾数から最初のマガジン分を減らす
            }
            else if (!Machine.InfiniteAmmo) //マガジンを使わず、かつ弾数無限がオフなら総弾数に代入
            {
                adshootingbehavour.AmmoLeft = ammo;
            }

            if (StatMaster.isHosting || !StatMaster.isMP || StatMaster.isLocalSim)   //ホストorマルチでないorローカルシミュ
            {

                //エフェクトにスクリプトを貼り付ける
                effectpool = GameObject.Find("PManager").transform.Find("EffectPool");  //エフェクトはソロマルチに拘わらずPManager/EffectPoolに入っている

                foreach (Transform child in effectpool)
                {
                    if (child.name == "ExplosionEffect")
                    {
                        adexplosioneffect = child.gameObject.GetComponent<AdExplosionEffect>();

                        //ブロック名が同じならスクリプトを貼り付けてDamageとRadiusを調整
                        if (adshootingbehavour.BlockName == adexplosioneffect.BlockName)
                        {
                            snbadexplosioncontroller = child.gameObject.GetComponent<SNBAdExplosionController>();

                            if (snbadexplosioncontroller == null)
                            {
                                snbadexplosioncontroller = child.gameObject.AddComponent<SNBAdExplosionController>();
                            }

                            snbadexplosioncontroller.Damage = Damage;
                            snbadexplosioncontroller.Radius = Radius;
                        }
                    }
                }
            }
        }

    }


    //エフェクトに貼り付けられるスクリプト
    public class SNBAdExplosionController : AdExplosionEffect
    {
        public bool ParameterInit = false;
        public int Damage;
        public int Radius;
        public ushort ID;
        public LayerMask layermask = (1 << 0) | (1 << 12) | (1 << 14) | (1 << 25) | (1 << 26);
        public string blockname;
        public bool init;
        public Dictionary<ushort, bool> ApplyDict = new Dictionary<ushort, bool>
        {
            [0] = false,    //自傷ダメージをオフにするならここをtrueに
            [1] = false,
            [2] = false,
            [3] = false,
            [4] = false,
            [5] = false,
            [6] = false,
            [7] = false,
            [8] = false,
            [9] = false,
            [10] = false,
            [11] = false,
            [12] = false,
            [13] = false,
            [14] = false,
            [15] = false,
            [16] = false,
            [17] = false,
            [100] = true

        };

        //エフェクトは使い回されるのでOnDisableで初期化する
        public void OnDisable()
        {
            for (ushort i = 0; i < 18; i++)
            {
                ApplyDict[i] = false;
            }

            init = false;
        }

        public new void FixedUpdate()
        {
            if (!init)
            {
                if (!StatMaster.isMP || StatMaster.isHosting || StatMaster.isLocalSim)   //マルチでない or ホストである or ローカルシミュである
                {

                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, Radius, layermask);

                    foreach (Collider collider in hitColliders)
                    {
                        blockname = collider.gameObject.transform.parent.name;

                        switch (blockname)
                        {
                            case "Simulation Machine":  //コアブロ、ホイール、ユニバなど
                                ID = collider.gameObject.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;
                                break;


                            case "Colliders": //Modブロック
                                ID = collider.gameObject.transform.parent.transform.parent.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;
                                break;

                            default:
                                try
                                {
                                    ID = collider.gameObject.transform.parent.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;
                                }
                                catch
                                {
                                    ID = 100;
                                }
                                break;
                        }

                        if (!ApplyDict[ID])
                        {
                            Mod.HPDict[ID] -= Damage;
                            ApplyDict[ID] = true;
                        }
                    }
                }
                init = true;

            }

            base.FixedUpdate();
        }


    }
}
