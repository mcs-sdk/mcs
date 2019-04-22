using UnityEngine;
using System.Collections;

public class RuntimeTextureLoader
{
	public Texture2D LoadFromByteArray(byte[] textureByteArray){
		Texture2D tex = new Texture2D(2, 2);
		tex.LoadImage(textureByteArray);
		return tex;
	}
}

