using System;
using System.Collections.Generic;
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
		public MKey EndEffectKey;

		public GameObject EffectPrefab;
		public GameObject EffectObject;
		public ParticleSystem Effectparticlesystem;
		public ParticleSystem EndEffectparticlesystem;
		public override void OnSimulateStart()  //シミュ開始時
        {
			//爆発エフェクトを取得・子オブジェクトとして初期化
			EffectPrefab = Mod.modAssetBundle.LoadAsset<GameObject>("HPexplosion");
			EffectObject = (GameObject)Instantiate(EffectPrefab, transform);
			Effectparticlesystem = EffectObject.GetComponent<ParticleSystem>();
			Effectparticlesystem.Stop();
			EffectObject.transform.position = ;
		}
	}



}
