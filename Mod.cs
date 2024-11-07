using System;
using System.Collections.Generic;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.UI;


namespace StusNavalSpace
{
	public class Mod : ModEntryPoint
	{
		public static GameObject SNB_UI;    //Mod�S�̂̊Ǘ��p��GameObject
		public static Dictionary<int, int> HPDict;  //�I���S����HP���ۑ�����鎫��
		public static MessageType messageType1; //HP�𑗂�
		public static Message message1;

		public static Message message2;

		public static ModAssetBundle modAssetBundle;

		public static void Log(string msg)
		{
			Debug.Log("SNB Log: " + msg);
		}
		public static void Warning(string msg)
		{
			Debug.LogWarning("SNB Warning: " + msg);
		}
		public static void Error(string msg)
		{
			Debug.LogError("SNB Error: " + msg);
		}

		public override void OnLoad()
		{
			// Called when the mod is loaded.

			//�eModule��Behaviour���Z�b�g�ɂ��AXML��Ŏg����悤��
			Modding.Modules.CustomModules.AddBlockModule<SNBUIModule, SNBUIBehaviour>("SNBUIModule", true);
			Modding.Modules.CustomModules.AddBlockModule<SNBAmmoUIModule, SNBAmmoUIBehaviour>("SNBAmmoUIModule", true);
			Modding.Modules.CustomModules.AddBlockModule<SNBAdWeaponConfigModule, SNBAdWeaponConfigBehaviour>("SNBAdWeaponConfigModule", true);
			Modding.Modules.CustomModules.AddBlockModule<SNBEffectModule, SNBEffectBehaviour>("SNBEffectModule", true);
			Modding.Modules.CustomModules.AddBlockModule<SNBAdTropedConfiModule, SNBTropedBehaviour>("SNBAdTropedConfiModule", true);

			HPDict = new Dictionary<int, int> { };

			//SNB_UI���쐬�ACanvas��ǉ�
			SNB_UI = new GameObject("SNB UI");
			UnityEngine.Object.DontDestroyOnLoad(SNB_UI);
			Canvas val = SNB_UI.AddComponent<Canvas>();
			val.renderMode = 0;
			val.sortingOrder = 0;
			val.gameObject.layer = LayerMask.NameToLayer("HUD");
			SNB_UI.AddComponent<CanvasScaler>().scaleFactor = 1f;   //��ʃT�C�Y�ɉ�����UI���X�P�[�����O���邽�߂̃R���|�[�l���g���A�^�b�`����


			//AssetBundle�̓ǂݍ���
			switch (Application.platform)   //OS���ɕύX
			{
				case RuntimePlatform.WindowsPlayer:
					modAssetBundle = ModResource.GetAssetBundle("SBNmyasset");
					break;
				case RuntimePlatform.OSXPlayer:
					modAssetBundle = ModResource.GetAssetBundle("SBNmyassetMac");
					break;
				case RuntimePlatform.LinuxPlayer:
					modAssetBundle = ModResource.GetAssetBundle("SBNmyassetMac");
					break;
				default:
					modAssetBundle = ModResource.GetAssetBundle("SBNmyasset");
					break;
			}


			//�N���C�A���g�ɑ��M���郁�b�Z�[�W�̌^�Ǝ󗝎�����̓o�^
			messageType1 = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
			ModNetworking.Callbacks[messageType1] += new Action<Message>(ApplyHP);

		}

		public static void ApplyHP(Message message) //�z�X�g���瑗���Ă����l��HP���X�V
		{
			HPDict[(int)message.GetData(0)] = (int)message.GetData(1);
		}

	}

}
