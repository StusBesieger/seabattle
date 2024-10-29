using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;
using Modding;
using Modding.Serialization;
using Modding.Modules;
using Modding.Blocks;
using skpCustomModule;


namespace StusNavalSpace
{
    [XmlRoot("SNBEffectModule")]
    [Reloadable]
    public class SNBEffectModule : BlockModule
    {

		[XmlElement("EndEffectKey")]
		[RequireToValidate]
		public MKeyReference EndEffectKey;

		[XmlElement("EffectPosition")]
		[DefaultValue(null)]
		[CanBeEmpty]
		public TransformValues EffectPosition;
	}
	public class SNBEffectBehaviour : BlockModuleBehaviour<SNBEffectModule>
    {
		public int blockID;
		public MKey EndEffectKey;
		public Transform effectposition;
		public GameObject EffectPrefab;
		public GameObject EffectObject;
		public GameObject EndEffectPrefab;
		public GameObject EndEffectObject;
		public ParticleSystem Effectparticlesystem;
		public ParticleSystem EndEffectparticlesystem;
		private AdTransformValues EffectPosition = new AdTransformValues();
		public override void OnSimulateStart()  //シミュ開始時
        {
			//常時発生するエフェクトを取得・子オブジェクトとして初期化
			EffectPrefab = Mod.modAssetBundle.LoadAsset<GameObject>("UsuallyEffect");
			EffectObject = (GameObject)Instantiate(EffectPrefab, transform);
			Effectparticlesystem = EffectObject.GetComponent<ParticleSystem>();
			Effectparticlesystem.Stop();
			EffectObject.transform.localPosition = EffectPosition.Position;
			EffectObject.transform.localEulerAngles = EffectPosition.Rotation;
			EffectObject.transform.localScale = EffectPosition.Scale;

			//終了時に発生するエフェクトを取得・子オブジェクトとして初期化
			EndEffectPrefab = Mod.modAssetBundle.LoadAsset<GameObject>("EndEffect");
			EndEffectObject = (GameObject)Instantiate(EndEffectPrefab, transform);
			EndEffectparticlesystem = EndEffectObject.GetComponent<ParticleSystem>();
			EndEffectparticlesystem.Stop();
			EndEffectObject.transform.localPosition = EffectPosition.Position;
			EndEffectObject.transform.localEulerAngles = EffectPosition.Rotation;
			EndEffectObject.transform.localScale = EffectPosition.Scale;


		}
        public override void SafeAwake()
        {
            base.SafeAwake();
			blockID = BlockId;
			try
            {
				EndEffectKey = GetKey(Module.EndEffectKey);
            }
            catch
            {
				Mod.Error("BlockID" + blockID + "error");
            }
        }

        public IEnumerator SimulateUpdate()
        {
			bool flag = !this.Effectparticlesystem.isPlaying;
			if(flag)
            {
				this.Effectparticlesystem.Play();
            }
			yield return new WaitForSeconds(1f);
			bool isPlaying = this.Effectparticlesystem.isPlaying;
			if(isPlaying)
            {
				this.Effectparticlesystem.Stop();
            }

			if (EndEffectKey.IsPressed || EndEffectKey.EmulationPressed())
			{
				EndEffectparticlesystem.Play();
				yield return new WaitForSeconds(2f);
				EndEffectparticlesystem.Stop();
			}

		}
		
	}
}
