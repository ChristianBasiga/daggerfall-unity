// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using DaggerfallConnect;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects
{
    /// <summary>
    /// ElementalResistance Fire/Frost/Poison/Shock/Magic multi-effect
    /// </summary>
    public class ElementalResistance : IncumbentEffect
    {
        #region Fields

        const int totalVariants = 5;
        //const int savingThrowModifier = 75;
        //const string textDatabase = "ClassicEffects";
        readonly string[] subGroupTextKeys = { "fire", "frost", "poison", "shock", "magicka" };
        readonly VariantProperties[] variantProperties = new VariantProperties[totalVariants];

        #endregion

        #region Structs

        struct VariantProperties
        {
            public DFCareer.Elements elementResisted;
            public EffectProperties effectProperties;
        }

        #endregion

        #region Properties

        public override EffectProperties Properties
        {
            get { return variantProperties[currentVariant].effectProperties; }
        }

        public override bool ChanceSuccess
        {
            // Always allow effect to succeed startup - we want to use chance component in a custom way
            get { return true; }
        }

        public DFCareer.Elements ElementResisted
        {
            get { return variantProperties[currentVariant].elementResisted; }
        }

        #endregion

        #region Overrides

        public override void SetProperties()
        {
            // Set properties shared by all variants
            properties.GroupName = TextManager.Instance.GetText("ClassicEffects", "elementalResistance");
            properties.SupportDuration = true;
            properties.SupportChance = true;
            properties.AllowedTargets = EntityEffectBroker.TargetFlags_All;
            properties.AllowedElements = EntityEffectBroker.ElementFlags_MagicOnly;
            properties.AllowedCraftingStations = MagicCraftingStations.SpellMaker;
            properties.MagicSkill = DFCareer.MagicSkills.Alteration;
            properties.DurationCosts = MakeEffectCosts(100, 100);
            properties.ChanceCosts = MakeEffectCosts(8, 100);

            // Set unique variant properties
            variantCount = totalVariants;
            SetVariantProperties(DFCareer.Elements.Fire);
            SetVariantProperties(DFCareer.Elements.Frost);
            SetVariantProperties(DFCareer.Elements.DiseaseOrPoison);
            SetVariantProperties(DFCareer.Elements.Shock);
            SetVariantProperties(DFCareer.Elements.Magic);
        }

        protected override bool IsLikeKind(IncumbentEffect other)
        {
            return (other is ElementalResistance && (other as ElementalResistance).ElementResisted == ElementResisted) ? true : false;
        }

        protected override void AddState(IncumbentEffect incumbent)
        {
            // Stack my rounds onto incumbent
            incumbent.RoundsRemaining += RoundsRemaining;
        }

        public override void ConstantEffect()
        {
            base.ConstantEffect();
            StartResisting();
        }

        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);
            StartResisting();
        }

        public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Resume(effectData, manager, caster);
            StartResisting();
        }

        public override void End()
        {
            base.End();
            StopResisting();
        }

        #endregion

        #region Private Methods

        void SetVariantProperties(DFCareer.Elements element)
        {
            int variantIndex = (int)element;
            string name = TextManager.Instance.GetText("ClassicEffects", subGroupTextKeys[variantIndex]);

            VariantProperties vp = new VariantProperties();
            vp.effectProperties = properties;
            vp.effectProperties.Key = string.Format("ElementalResistance-{0}", name);
            vp.effectProperties.ClassicKey = MakeClassicKey(8, (byte)variantIndex);
            vp.effectProperties.SubGroupName = name;
            vp.effectProperties.SpellMakerDescription = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(1527 + variantIndex);
            vp.effectProperties.SpellBookDescription = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(1227 + variantIndex);
            vp.elementResisted = element;
            variantProperties[variantIndex] = vp;
        }

        void StartResisting()
        {
            // Get peered entity gameobject
            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            entityBehaviour.Entity.SetResistanceFlag(variantProperties[currentVariant].elementResisted, true);
            entityBehaviour.Entity.SetResistanceChance(variantProperties[currentVariant].elementResisted, ChanceValue());
        }

        void StopResisting()
        {
            // Get peered entity gameobject
            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            entityBehaviour.Entity.SetResistanceFlag(variantProperties[currentVariant].elementResisted, false);
            entityBehaviour.Entity.SetResistanceChance(variantProperties[currentVariant].elementResisted, 0);
        }

        #endregion  
    }
}
