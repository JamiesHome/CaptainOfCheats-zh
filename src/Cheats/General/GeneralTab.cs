﻿using System;
using System.Collections.Generic;
using CaptainOfCheats.Extensions;
using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Syncers;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using Mafi.Unity.UiFramework;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UiFramework.Components.Tabs;
using Mafi.Unity.UserInterface.Components;
using UnityEngine;

namespace CaptainOfCheats.Cheats.General
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class GeneralTab : Tab, ICheatProviderTab
    {
        private readonly UnityCheatProvider _unityCheatProvider;
        private readonly ResearchCheatProvider _researchCheatProvider;
        private readonly PopulationCheatProvider _populationCheatProvider;
        private readonly MaintenanceCheatProvider _maintenanceCheatProvider;
        private readonly InstantBuildCheatProvider _instantBuildCheatProvider;
        private readonly DiseaseCheatProvider _diseaseCheatProvider;
        private readonly Dict<SwitchBtn, Func<bool>> _switchBtns = new Dict<SwitchBtn, Func<bool>>();

        public GeneralTab(NewInstanceOf<InstantBuildCheatProvider> instantBuildCheatProvider,
            NewInstanceOf<MaintenanceCheatProvider> maintenanceCheatProvider,
            NewInstanceOf<PopulationCheatProvider> populationCheatProvider,
            NewInstanceOf<ResearchCheatProvider> researchCheatProvider,
            NewInstanceOf<UnityCheatProvider> unityCheatProvider,
            NewInstanceOf<DiseaseCheatProvider> diseaseCheatProvider
        ) : base(nameof(GeneralTab), SyncFrequency.OncePerSec)
        {
            _unityCheatProvider = unityCheatProvider.Instance;
            _researchCheatProvider = researchCheatProvider.Instance;
            _populationCheatProvider = populationCheatProvider.Instance;
            _maintenanceCheatProvider = maintenanceCheatProvider.Instance;
            _instantBuildCheatProvider = instantBuildCheatProvider.Instance;
            _diseaseCheatProvider = diseaseCheatProvider.Instance;
        }

        public string Name => "居民服务";
        public string IconPath => Assets.Unity.UserInterface.Toolbar.Settlement_svg;

        private Dictionary<int, Action<int>> PopulationIncrementButtonConfig =>
            new Dictionary<int, Action<int>>
            {
                { 5, _populationCheatProvider.ChangePopulation },
                { 25, _populationCheatProvider.ChangePopulation },
                { 50, _populationCheatProvider.ChangePopulation }
            };


        public override void RenderUpdate(GameTime gameTime)
        {
            RefreshValues();
            base.RenderUpdate(gameTime);
        }

        public override void SyncUpdate(GameTime gameTime)
        {
            RefreshValues();
            base.SyncUpdate(gameTime);
        }

        private void RefreshValues()
        {
            foreach (var kvp in _switchBtns) kvp.Key.SetIsOn(kvp.Value());
        }

        protected override void BuildUi()
        {
            var tabContainer = Builder
                .NewStackContainer("tabContainer")
                .SetStackingDirection(StackContainer.Direction.TopToBottom)
                .SetSizeMode(StackContainer.SizeMode.Dynamic)
                .SetInnerPadding(Offset.All(15f))
                .SetItemSpacing(5f)
                .PutToTopOf(this, 0.0f);

            var firstRowContainer = Builder
                .NewStackContainer("firstRowContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .AppendTo(tabContainer, offset: Offset.All(0), size: 30);

            var instantModeToggle = NewToggleSwitch(
                "即时生效模式",
                "将即时模式设置为关闭（未选中）或打开（选中）。启用即时建造、即时研究、即时升级（造船厂、建筑物、定居点、矿山）、即时车辆建造和即时维修.",
                toggleVal => _instantBuildCheatProvider.ToggleInstantMode(toggleVal),
                () => _instantBuildCheatProvider.IsInstantModeEnabled());
            instantModeToggle.AppendTo(firstRowContainer, new Vector2(instantModeToggle.GetWidth(), 25), ContainerPosition.LeftOrTop);

            var maintenanceToggle = NewToggleSwitch(
                "维护",
                "将维护设置为关闭（未选中）或打开（选中）。如果打开，那么您的结算将消耗维护资源。如果关闭，所有维护消耗将停止",
                toggleVal => _maintenanceCheatProvider.ToggleMaintenance(toggleVal),
                () => _maintenanceCheatProvider.IsMaintenanceEnabled());
            maintenanceToggle.AppendTo(firstRowContainer, new Vector2(maintenanceToggle.GetWidth(), 25), ContainerPosition.LeftOrTop);
            maintenanceToggle.PutToRightOf(instantModeToggle, maintenanceToggle.GetWidth());
            
            var diseaseToggle = NewToggleSwitch(
                "健康",
                "将“健康”设置为关闭（未选中）或打开（选中）。如果关闭，每天如果检测到疾病，它将自动删除。切换开/关不会保留在您的保存游戏中，并且会重置每次重新加载.",
                toggleVal => _diseaseCheatProvider.ToggleDisease(toggleVal),
                () => !_diseaseCheatProvider.IsDiseaseDisabled);
            diseaseToggle.AppendTo(firstRowContainer, new Vector2(diseaseToggle.GetWidth(), 25), ContainerPosition.LeftOrTop);
            diseaseToggle.PutToRightOf(instantModeToggle, diseaseToggle.GetWidth(), Offset.Right(-225));

            
            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("定居人口"), new LocStrFormatted("使用增量按钮在人口中添加或删除人员."));
            var populationIncrementButtonGroup = Builder.NewIncrementButtonGroup(PopulationIncrementButtonConfig);
            populationIncrementButtonGroup.AppendTo(tabContainer, new float?(50f), Offset.All(0));


            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("研究"));
            var researchPanel = Builder.NewPanel("researchPanel").SetBackground(Builder.Style.Panel.ItemOverlay);

            researchPanel.AppendTo(tabContainer, size: 50f, Offset.All(0));

            var thirdRowContainer = Builder
                .NewStackContainer("thirdRowContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .PutToLeftOf(researchPanel, 0.0f, Offset.Left(10f));
            
            var unlockCurrentResearchButton = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("完成当前研究"))
                .AddToolTip("开始研究，然后使用这个命令立即完成它。您还可以使用即时模式立即完成已开始的研究.")
                .OnClick(_researchCheatProvider.UnlockCurrentResearch);
            unlockCurrentResearchButton.AppendTo(thirdRowContainer, unlockCurrentResearchButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);

            var unlockAllResearchButton = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("解锁完成所有研究"))
                .AddToolTip("解锁所有研究，包括需要发现才能进行研究的研究.")
                .OnClick(_researchCheatProvider.UnlockAllResearch);
            unlockAllResearchButton.AppendTo(thirdRowContainer, unlockAllResearchButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("其他"));
            var otherPanel = Builder.NewPanel("otherPanel").SetBackground(Builder.Style.Panel.ItemOverlay);
            otherPanel.AppendTo(tabContainer, size: 50f, Offset.All(0));
            
            var fourthRowContainer = Builder
                .NewStackContainer("fourthRowContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .PutToLeftOf(otherPanel, 0.0f, Offset.Left(10f));
            
            var addUnityButton = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("ADD 25"))
                .AddToolTip("将 25 凝聚力 添加到您当前的供应量中，不会超出您的最大 凝聚力 上限")
                .OnClick(() => _unityCheatProvider.AddUnity(25));
            addUnityButton.AppendTo(fourthRowContainer, addUnityButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            RefreshValues();
        }

        private SwitchBtn NewToggleSwitch(string text, string tooltip, Action<bool> onToggleAction, Func<bool> isToggleEnabled)
        {
            var toggleBtn = Builder.NewSwitchBtn()
                .SetText(text)
                .AddTooltip(new LocStrFormatted(tooltip))
                .SetOnToggleAction(onToggleAction);

            _switchBtns.Add(toggleBtn, isToggleEnabled);

            return toggleBtn;
        }
    }
}