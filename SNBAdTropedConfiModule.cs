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

namespace StusNavalSpace
{
    [XmlRoot("SNBAdTropedConfiModule")]
    [Reloadable]
    public class SNBAdTropedConfiModule : BlockModule
    {
        [XmlElement("Tropedaltitude")]
        [RequireToValidate]
        public MSliderReference AltitudeSlider;
    }

    public class SNBTropedBehaviour : BlockModuleBehaviour<SNBAdTropedConfiModule>
    {
        private AdShootingBehavour adshootingbehavour;
        private AdProjectileScript adprojectilescript;
        private GameObject projectilepool;
        private SNBTropedController snbtropedcontroller;
        public MSlider altitudeslider;
        public float Altitude;

        public override void OnSimulateStart()
        {
            base.OnSimulateStart();

            adshootingbehavour = GetComponent<AdShootingBehavour>();

            altitudeslider = GetSlider(Module.AltitudeSlider);

            Altitude = (float)altitudeslider.Value;

            Debug.Log("Start");
            if (StatMaster.isHosting || !StatMaster.isMP || StatMaster.isLocalSim)   //ホストorマルチでないorローカルシミュ
            {

                //弾にスクリプトを貼り付ける
                projectilepool = GameObject.Find("PHYSICS GOAL");  

                foreach (Transform child in projectilepool.transform)
                {
                    Debug.Log("projectilepool");
                    if (child.name == "AdProjectile(Clone)(Clone)")
                    {
                        Debug.Log("childname");
                        adprojectilescript = child.gameObject.GetComponent<AdProjectileScript>();

                        //ブロック名が同じならスクリプトを貼り付ける
                        if (adshootingbehavour.BlockName == adprojectilescript.BlockName)
                        {
                            Debug.Log("Script");
                            snbtropedcontroller = child.gameObject.GetComponent<SNBTropedController>();

                            if (snbtropedcontroller == null)
                            {
                                Debug.Log("harituke");
                                snbtropedcontroller = child.gameObject.AddComponent<SNBTropedController>();
                            }
                            snbtropedcontroller.Altitude = Altitude;
                        }
                    }
                }
            }
        }
    }
    public class SNBTropedController : MonoBehaviour
    {
        private AdShootingBehavour adshootingbehavour;
        public float Altitude;
        private Rigidbody rigidbody;
        public void OnSimulateStart()
        {
            Debug.Log("simulateScript");
        }
        public void OnEnabel()
        {
            Debug.Log("freez");
            rigidbody = GetComponent<Rigidbody>();
            this.rigidbody.constraints = RigidbodyConstraints.FreezeRotationY;
        }

        public void SimulateUpdateAlways()
        {
           
            Vector3 tropedposition = this.transform.position;
            Vector3 tropedrotation = this.transform.eulerAngles;
            Vector3 upforce = new Vector3(0.0f, 5.0f, 0.0f);
            Vector3 downforce = new Vector3(0.0f, -5.0f, 0.0f);

            if (tropedposition.y > Altitude + 0.25)
            {
                rigidbody.AddForce(downforce, ForceMode.Force);
            }
            if (tropedposition.y < Altitude - 0.25)
            {
                rigidbody.AddForce(upforce, ForceMode.Force);
            }
            if (tropedrotation.x != 0 || tropedrotation.z != 0)
            {
                this.transform.rotation = Quaternion.Euler(0.0f, tropedrotation.y, 0.0f);
            }
        }
    }
}
