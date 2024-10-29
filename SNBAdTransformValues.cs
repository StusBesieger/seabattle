using System;
using Modding.Serialization;
using UnityEngine;
using Vector3 = Modding.Serialization.Vector3;

namespace StusNavalSpace
{
	
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
            Vector3 position = transformV.Position;
			Vector3 rotation = transformV.Rotation;
			Vector3 scale = transformV.Scale;
			return new AdTransformValues
			{
				Position = position,
				Rotation = rotation,
				Scale = scale
			};
		}

		
		public Vector3 Position;

		
		public Vector3 Rotation;

		
		public Vector3 Scale;

		
		private bool hasScale = true;
	}
}
