using System;
using MCS.Utility.Schematic.Enumeration;
using UnityEngine;


namespace MCS.Utility.Schematic.Base
{
	[Serializable]
	public class OriginAndDescription : ISerializationCallbackReceiver
	{
		#region Origin
		public string mcs_id;
		public string name;
		public string vendor_id;
		public string vendor_name;
		public string collection_id;
		public string collection_name;
		#region Artist Tools Specific. 
        [NonSerialized]
		public Guid mcs_id_guid;
        [NonSerialized]
		public Guid parent_id_guid;
		public string parent_id;
		public int child_count;
		#endregion
		#endregion
		#region Description

		[SerializeField]
		[HideInInspector]
		private string gender_enum;
		[ATEditable]
		[ATListView(typeof(Gender))]
		public Gender gender;

		[SerializeField]
		[HideInInspector]
		private string category_enum;
		[ATEditable]
		[ATListView(typeof(Category))]
		public Category category;

		[ATEditable]
		[ATInputText]
		public string[] tags;
		[ATEditable]
		[ATInputText]
		public string description;
        #endregion

        #region ISerializationCallbackReceiver implementation

        public void OnBeforeSerialize()
        {
            if (String.IsNullOrEmpty(mcs_id) && (mcs_id_guid != Guid.Empty))
            {
                mcs_id = mcs_id_guid.ToString();
            }
            if (String.IsNullOrEmpty(parent_id) && (parent_id_guid != Guid.Empty))
            { 
                parent_id = parent_id_guid.ToString();
            }
			gender_enum = gender.ToString ();
			category_enum = category.ToString ();
		}

		public void OnAfterDeserialize ()
		{
			if (!String.IsNullOrEmpty(mcs_id))
                mcs_id_guid = new Guid(mcs_id);
			if (!String.IsNullOrEmpty(parent_id))
				parent_id_guid = new Guid(parent_id);
			
			if (gender_enum != null) {
				gender = (Gender)Enum.Parse (typeof(Gender), gender_enum);
			}
			if (category_enum != null) {
				category = (Category)Enum.Parse (typeof(Category), category_enum);
			}
		}

		#endregion
	}
}

