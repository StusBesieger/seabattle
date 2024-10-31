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
using Vector3 = UnityEngine.Vector3;

namespace StusNavalSpace
{
    [XmlRoot("SNBEffectModule")]
    [Reloadable]
    public class SNBEffectModule : BlockModule
    {

		[XmlElement("EndEffectKey")]
		[RequireToValidate]
		public MKeyReference EndEffectKey;

		[XmlElement("EffectPositionX")]
		[DefaultValue(0f)]
		[Reloadable]
		public float EffectPositionX;

		[XmlElement("EffectPositionY")]
		[DefaultValue(0f)]
		[Reloadable]
		public float EffectPositionY;

		[XmlElement("EffectPositionZ")]
		[DefaultValue(0f)]
		[Reloadable]
		public float EffectPositionZ;

		[XmlElement("EffectRotationX")]
		[DefaultValue(0f)]
		[Reloadable]
		public float EffectRotationX;

		[XmlElement("EffectRotationY")]
		[DefaultValue(0f)]
		[Reloadable]
		public float EffectRotationY;

		[XmlElement("EffectRotationZ")]
		[DefaultValue(0f)]
		[Reloadable]
		public float EffectRotationZ;
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
		private float EffectPositionX;
		private float EffectPositionY;
		private float EffectPositionZ;
		private float EffectRotationX;
		private float EffectRotationY;
		private float EffectRotationZ;

		public override void OnSimulateStart()  //シミュ開始時
        {
			//エフェクトの位置と回転を代入するための準備
			this.EffectPositionX = -Module.EffectPositionX;
			this.EffectPositionY = Module.EffectPositionY;
			this.EffectPositionZ = Module.EffectPositionZ;
			this.EffectRotationX = Module.EffectRotationX;
			this.EffectRotationY = -Module.EffectRotationY;
			this.EffectRotationZ = Module.EffectRotationZ;
			Vector3 EffectPosition = new Vector3(EffectPositionX, EffectPositionY, EffectPositionZ);
			Vector3 EffectRotation = new Vector3(EffectRotationX, EffectRotationY, EffectRotationZ);

			//常時発生するエフェクトを取得・子オブジェクトとして初期化
			EffectPrefab = Mod.modAssetBundle.LoadAsset<GameObject>("UsuallyEffect");
			EffectObject = (GameObject)Instantiate(EffectPrefab, transform);
			Effectparticlesystem = EffectObject.GetComponent<ParticleSystem>();
			EffectObject.transform.localPosition = EffectPosition;
			EffectObject.transform.localRotation = Quaternion.Euler(EffectRotation);

			//終了時に発生するエフェクトを取得・子オブジェクトとして初期化
			EndEffectPrefab = Mod.modAssetBundle.LoadAsset<GameObject>("EndEffect");
			EndEffectObject = (GameObject)Instantiate(EndEffectPrefab, transform);
			EndEffectparticlesystem = EndEffectObject.GetComponent<ParticleSystem>();
			EndEffectparticlesystem.Stop();
			EndEffectObject.transform.localPosition = EffectPosition;
			EndEffectObject.transform.localRotation = Quaternion.Euler(EffectRotation);


			//常時発生するエフェクトのループをonにし、生成させる。
			this.Effectparticlesystem.loop = true;
			this.Effectparticlesystem.Play();
		}
		//キーの取得
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
		//キーが押されている時終了エフェクト関数を呼び出す
		public override void SimulateUpdateAlways()
		{
			base.SimulateUpdateAlways();

			if (EndEffectKey.IsPressed || EndEffectKey.EmulationPressed())
			{
				StartCoroutine(PlayEndEffect());
	
			}

		}	
		//シミュ停止時に常時生成するエフェクトを終了させる
		public override void OnSimulateStop()
        {
			this.Effectparticlesystem.Stop();
			this.Effectparticlesystem.loop = false;
		}
		//終了エフェクトの生成と常時発生エフェクトの停止
		public IEnumerator PlayEndEffect()
        {
			yield return new WaitForSeconds(1f);
			EndEffectparticlesystem.Play();
			this.Effectparticlesystem.Stop();
			yield return new WaitForSeconds(0.5f);
			this.Effectparticlesystem.loop = false;
			yield return new WaitForSeconds(10f);
			EndEffectparticlesystem.Stop();
		}
	}
}
