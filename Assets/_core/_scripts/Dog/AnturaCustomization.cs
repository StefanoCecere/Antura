using Antura.Core;
using Antura.Database;
using Antura.Rewards;
using System;
using System.Collections.Generic;
using System.Linq;
using Antura.AnturaSpace.UI;
using UnityEngine;

namespace Antura.Dog
{
    [Serializable]
    public class AllPetsAnturaCustomization
    {
        public AnturaCustomization[] Customizations = new AnturaCustomization[0];
        public string GetJsonListOfIds()
        {
            return JsonUtility.ToJson(this);
        }

        public void Append(AnturaCustomization customization)
        {
            var list = Customizations.ToList();
            list.RemoveAll(x => x.PetType == customization.PetType);
            list.Add(customization);
            Customizations = list.ToArray();
        }
    }

    /// <summary>
    /// Saved data that defines how Antura is currently customized
    /// </summary>
    [Serializable]
    public class AnturaCustomization
    {
        public AnturaPetType PetType = AnturaPetType.Dog;

        [NonSerialized]
        public List<RewardPack> PropPacks = new List<RewardPack>();
        public List<string> PropPacksIds = new List<string>();

        [NonSerialized]
        public RewardPack TexturePack = null;
        public string TexturePackId = null;

        [NonSerialized]
        public RewardPack DecalPack = null;
        public string DecalPackId = null;

        /// <summary>
        /// Loads all rewards in "this" object instance from list of reward ids.
        /// </summary>
        /// <param name="_listOfIdsAsJsonString">The list of ids as json string.</param>
        public void LoadFromListOfIds(string _listOfIdsAsJsonString)
        {
            //Debug.LogError("LOADING PET " + PetType);
            if (AppManager.I.Player == null)
            {
                Debug.Log("No default reward already created. Unable to load customization now");
                return;
            }

            AnturaCustomization customization = null;
            try
            {
                var allPetsAnturaCustomization = JsonUtility.FromJson<AllPetsAnturaCustomization>(_listOfIdsAsJsonString);
                if (allPetsAnturaCustomization.Customizations.Length == 0)
                {
                    throw new Exception("Old customization detected");
                }
                customization = allPetsAnturaCustomization.Customizations.FirstOrDefault(x => x.PetType == PetType);
            }
            catch (Exception)
            {
                if (DebugConfig.I.VerboseAntura)
                    Debug.LogWarning("Old customization detected. Upgrading it.");
                var dogCustomization = JsonUtility.FromJson<AnturaCustomization>(_listOfIdsAsJsonString);
                if (PetType == AnturaPetType.Dog)
                {
                    customization = dogCustomization;
                    AppManager.I.Player.MigrateOldCustomization(customization);
                }
            }

            if (customization != null)
            {
                PropPacksIds = customization.PropPacksIds;
                TexturePackId = customization.TexturePackId;
                DecalPackId = customization.DecalPackId;
            }

            var rewardSystem = AppManager.I.RewardSystemManager;

            if (string.IsNullOrEmpty(TexturePackId))
            {
                RewardPack defaultTileTexturePack = rewardSystem.GetAllRewardPacksOfBaseType(RewardBaseType.Texture, petType: PetType)[0];
                if (DebugConfig.I.VerboseAntura)
                    Debug.LogWarning("AnturaCustomization: Using default texture: " + defaultTileTexturePack);
                TexturePackId = defaultTileTexturePack.UniqueId;
            }
            if (string.IsNullOrEmpty(DecalPackId))
            {
                RewardPack defaultDecalTexturePack = rewardSystem.GetAllRewardPacksOfBaseType(RewardBaseType.Decal, petType: PetType)[0];
                if (DebugConfig.I.VerboseAntura)
                    Debug.LogWarning("AnturaCustomization: Using default decal: " + defaultDecalTexturePack);
                DecalPackId = defaultDecalTexturePack.UniqueId;
            }

            // Load correct packs from IDs
            PropPacks = new List<RewardPack>();
            foreach (string propPackId in PropPacksIds)
            {
                var pack = rewardSystem.GetRewardPackByUniqueId(propPackId, PetType);
                if (pack != null)
                {
                    PropPacks.Add(pack);
                }
                else
                {
                    Debug.LogError("Null pack with id " + propPackId);
                }
            }

            TexturePack = rewardSystem.GetRewardPackByUniqueId(TexturePackId, PetType);
            if (TexturePack == null)
            {
                RewardPack defaultTileTexturePack = rewardSystem.GetAllRewardPacksOfBaseType(RewardBaseType.Texture, petType: PetType)[0];
                Debug.LogWarning($"AnturaCustomization: Could not find TexturePackID {TexturePackId}. Using default texture: " + defaultTileTexturePack);
                TexturePackId = defaultTileTexturePack.UniqueId;
                TexturePack = defaultTileTexturePack;
            }

            DecalPack = rewardSystem.GetRewardPackByUniqueId(DecalPackId, PetType);
            if (string.IsNullOrEmpty(DecalPackId))
            {
                RewardPack defaultDecalTexturePack = rewardSystem.GetAllRewardPacksOfBaseType(RewardBaseType.Decal, petType: PetType)[0];
                Debug.LogWarning($"AnturaCustomization: Could not find DecalPackId {DecalPackId}. Using default texture: " + defaultDecalTexturePack);
                DecalPackId = defaultDecalTexturePack.UniqueId;
                DecalPack = defaultDecalTexturePack;
            }
        }

        /// <summary>
        /// Return all rewards objects to json list of ids (to be stored on db).
        /// </summary>
        public string GetJsonListOfIds()
        {
            return JsonUtility.ToJson(this);
        }

        public bool HasBaseEquipped(string baseId)
        {
            if (PropPacks.Exists(f => f.BaseId == baseId))
                return true;
            if (DecalPack.BaseId == baseId)
                return true;
            if (TexturePack.BaseId == baseId)
                return true;
            return false;
        }

        public bool HasSomethingEquipped(AnturaSpaceCategoryButton.AnturaSpaceCategory category)
        {
            return PropPacks.Exists(f => f.Category == category.ToString());
        }

        public RewardPack GetEquippedPack(string baseId)
        {
            if (PropPacks.Exists(f => f.BaseId == baseId))
                return PropPacks.FirstOrDefault(f => f.BaseId == baseId);
            if (DecalPack.BaseId == baseId)
                return DecalPack;
            if (TexturePack.BaseId == baseId)
                return TexturePack;
            return null;
        }

        public void ClearEquippedProps()
        {
            PropPacks.Clear();
            PropPacksIds.Clear();
            AppManager.I.Player.SaveAnturaCustomization();
        }
    }
}
