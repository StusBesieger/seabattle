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
		public Transform effectposition;
		public GameObject EffectPrefab;
		public GameObject EffectObject;
		public ParticleSystem Effectparticlesystem;
		public ParticleSystem EndEffectparticlesystem;
		private AdTransformValues EffectPosition = new AdTransformValues();
		public override void OnSimulateStart()  //シミュ開始時
        {
			//爆発エフェクトを取得・子オブジェクトとして初期化
			EffectPrefab = Mod.modAssetBundle.LoadAsset<GameObject>("HPexplosion");
			EffectObject = (GameObject)Instantiate(EffectPrefab, transform);
			Effectparticlesystem = EffectObject.GetComponent<ParticleSystem>();
			Effectparticlesystem.Stop();
			EffectObject.transform.position = EffectPosition.Position;
			EffectObject.transform.rotation = EffectPosition.Rotation;

		}
	}

	public class AdTransformValues
	{

		public void SetOnTransform(Transform t)
		{
			t.localPosition = this.Position;
			t.localRotation = Quaternion.Euler(this.Rotation);
			bool flag = this.hasScale;
			if (flag)
			{
				t.localScale = this.Scale;
			}
		}


		public void FlipTransform()
		{
			this.Position.x = -1f * this.Position.x;
			this.Rotation.y = -1f * this.Rotation.y;
			this.Scale.x = -1f * this.Scale.x;
		}

		public static implicit operator AdTransformValues(TransformValues transformV)
		{
			Modding.Serialization.Vector3 position = transformV.Position;
			Modding.Serialization.Vector3 rotation = transformV.Rotation;
			Modding.Serialization.Vector3 scale = transformV.Scale;
			return new AdTransformValues
			{
				Position = position,
				Rotation = rotation,
				Scale = scale
			};
		}

		// Token: 0x040003A7 RID: 935
		public Modding.Serialization.Vector3 Position;

		// Token: 0x040003A8 RID: 936
		public Modding.Serialization.Vector3 Rotation;

		// Token: 0x040003A9 RID: 937
		public Modding.Serialization.Vector3 Scale;

		// Token: 0x040003AA RID: 938
		private bool hasScale = true;
	}


}
