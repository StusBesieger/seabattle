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
using Vector3 = UnityEngine.Vector3;
using System.Collections;

namespace StusNavalSpace
{
    [XmlRoot("SNBAdTropedConfiModule")]
    [Reloadable]
    public class SNBAdTropedConfiModule : BlockModule
    {
        [XmlElement("Tropedaltitude")]
        [RequireToValidate]
        public MSliderReference AltitudeSlider;
        [XmlElement("TropedBoosterPower")]
        [RequireToValidate]
        public MSliderReference TropedPowerSlider;
    }

    public class SNBTropedBehaviour : BlockModuleBehaviour<SNBAdTropedConfiModule>
    {
        private AdShootingBehavour adshootingbehavour;
        private AdProjectileScript adprojectilescript;
        private GameObject projectilepool;
        private Transform projectilmultipool;
        private SNBTropedController snbtropedcontroller;
        public MSlider altitudeslider;
        public MSlider tropedpowerslider;
        public float Altitude;
        public float Tropedpower;
        public float TropedWaterEffectHight => AdCustomModuleMod.skyBoxChanger.WaterHeight;
        public float TropedEffectposition;
        public override void OnSimulateStart()
        {
            base.OnSimulateStart();

            adshootingbehavour = GetComponent<AdShootingBehavour>();

            altitudeslider = GetSlider(Module.AltitudeSlider);

            tropedpowerslider = GetSlider(Module.TropedPowerSlider);

            Altitude = (float)altitudeslider.Value;

            Tropedpower = (float)tropedpowerslider.Value;
            TropedEffectposition = Altitude - TropedWaterEffectHight;


            if (StatMaster.isHosting || !StatMaster.isMP || StatMaster.isLocalSim)   //ホストorマルチでないorローカルシミュ
            {

                //弾にスクリプトを貼り付ける

                //レベルエディタ、マルチ以外
                projectilepool = GameObject.Find("PHYSICS GOAL");  

                foreach (Transform child in projectilepool.transform)
                {

                    if (child.name == "AdProjectile(Clone)(Clone)")
                    {

                        adprojectilescript = child.gameObject.GetComponent<AdProjectileScript>();

                        //ブロック名が同じならスクリプトを貼り付ける
                        if (adshootingbehavour.BlockName == adprojectilescript.BlockName)
                        {

                            snbtropedcontroller = child.gameObject.GetComponent<SNBTropedController>();

                            if (snbtropedcontroller == null)
                            {

                                snbtropedcontroller = child.gameObject.AddComponent<SNBTropedController>();
                            }
                            snbtropedcontroller.Altitude = Altitude;
                            snbtropedcontroller.TropedPower = Tropedpower*2;
                            snbtropedcontroller.TropedEffectPosition = TropedEffectposition;

                        }
                    }
                }

                //レベルエディタ、マルチのとき
                projectilmultipool = GameObject.Find("PManager").transform.Find("Projectile Pool");

                foreach (Transform child in projectilmultipool.transform)
                {

                    if (child.name == "AdProjectile(Clone)(Clone)")
                    {

                        adprojectilescript = child.gameObject.GetComponent<AdProjectileScript>();

                        //ブロック名が同じならスクリプトを貼り付ける
                        if (adshootingbehavour.BlockName == adprojectilescript.BlockName)
                        {

                            snbtropedcontroller = child.gameObject.GetComponent<SNBTropedController>();

                            if (snbtropedcontroller == null)
                            {

                                snbtropedcontroller = child.gameObject.AddComponent<SNBTropedController>();
                            }
                            snbtropedcontroller.Altitude = Altitude;
                            snbtropedcontroller.TropedPower = Tropedpower * 4;
                            snbtropedcontroller.TropedEffectPosition = TropedEffectposition;
                        }
                    }
                }

            }
        }
    }
    public class SNBTropedController : ProjectileScript
    {
        private AdShootingBehavour adshootingbehavour;
        private AdProjectileScript adProjectileScript;
        private GameObject GyroChild;
        private GameObject BulletEffectPrefab;
        public float Altitude;
        public float TropedPower;
        private Rigidbody rigidbody;
        private Vector3 tropedstartrotation;
        private Vector3 upforce = new Vector3(0.0f, 100.0f, 0.0f);
        private Vector3 downforce = new Vector3(0.0f, -200.0f, 0.0f);
        public float TropedEffectPosition;
        public ParticleSystem Tropedparticlesystem;
        public void Awake()
        {
            base.Awake();
            rigidbody = GetComponent<Rigidbody>();
            adProjectileScript = GetComponent<AdProjectileScript>();
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            GyroChild = this.transform.GetChild(0).gameObject;

        }
        public override void FixedUpdate()
        {
            tropedstartrotation = this.transform.eulerAngles;
            Vector3 tropedposition = this.transform.position;
            Vector3 tropedrotation = this.transform.eulerAngles;
            if (tropedposition.y > Altitude + 0.25)
            {
                rigidbody.AddForce(downforce, ForceMode.Force);
            }
            if (tropedposition.y < Altitude - 0.25)
            {
                rigidbody.AddForce(upforce, ForceMode.Force);
            }
            if (tropedrotation.x != 0f || tropedrotation.z != 0f)
            {
                this.transform.rotation = Quaternion.AngleAxis(tropedstartrotation.y, new Vector3(0, 1, 0));
            }
            if (adProjectileScript.delayBoostertime > 0.1f)
            {
                if (tropedposition.y < Altitude + 0.25)
                {
                    if (tropedposition.y > Altitude - 0.25)
                    {
                        adProjectileScript.delayBoostertime = 0.1f;
                        adProjectileScript.boosterPower = TropedPower;
                    }
                }
            }
            if(adProjectileScript.delayBoostertime<0)
            {
                if (adProjectileScript.delayBoostertime > -0.2f)
                {
                    foreach (Transform child in GyroChild.transform)
                    {
                        if (child.name == "BulletEffectPrefab")
                        {
                            BulletEffectPrefab = child.gameObject;
                            BulletEffectPrefab.transform.localPosition = new Vector3(0f, -TropedEffectPosition+0.25f, 0f);
                            Tropedparticlesystem = BulletEffectPrefab.GetComponent<ParticleSystem>();
                            Tropedparticlesystem.loop = true;
                        }
                    }
                }
            }
        }
        //不思議なおまじない
        public void OnEnable()
        {
        }
        public void OnCollisionEnter()
        { }
        public void Update()
        { }
        public void ValidCollisionOrTrigger()
        { }
    }
}
