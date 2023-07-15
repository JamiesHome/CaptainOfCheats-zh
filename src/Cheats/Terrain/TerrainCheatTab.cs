using System;
using System.Linq;
using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Syncers;
using Mafi.Localization;
using Mafi.Unity.InputControl;
using Mafi.Unity.UiFramework;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UiFramework.Components.Tabs;
using Mafi.Unity.UserInterface.Components;
using UnityEngine;
using Assets = Mafi.Unity.Assets;

namespace CaptainOfCheats.Cheats.Terrain
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class TerrainCheatTab : Tab, ICheatProviderTab
    {
        private readonly ProtosDb _protosDb;
        private readonly TerrainCheatProvider _cheatProvider;
        private readonly Dict<SwitchBtn, Func<bool>> _switchBtns = new Dict<SwitchBtn, Func<bool>>();
        private readonly IOrderedEnumerable<LooseProductProto> _looseProductProtos;
        private bool _disableTerrainPhysicsOnMiningAndDumping = false;
        private ProductProto.ID? _selectedLooseProductProto;
        private bool _ignoreMineTowerDesignations = true;

        public TerrainCheatTab(NewInstanceOf<TerrainCheatProvider> cheatProvider, ProtosDb _protosDb
        ) : base(nameof(TerrainCheatTab), SyncFrequency.OncePerSec)
        {
            this._protosDb = _protosDb;
            _cheatProvider = cheatProvider.Instance;

            _looseProductProtos = _protosDb.Filter<LooseProductProto>(proto => proto.CanBeLoadedOnTruck && proto.CanBeOnTerrain).OrderBy(x => x);
        }

        public string Name => "地形";
        public string IconPath => Assets.Unity.UserInterface.Toolbar.Dumping_svg;

        protected override void BuildUi()
        {
            var tabContainer = CreateStackContainer();

            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("地形"), new LocStrFormatted("选择倾倒时要使用的地形."));
            var terrainSelector = BuildTerrainSelector(tabContainer);
            terrainSelector.AppendTo(tabContainer, new Vector2(150, 28f), ContainerPosition.LeftOrTop);
            var terrainPhysicsToggleSwitch = CreateTerrainPhysicsToggleSwitch();
            terrainPhysicsToggleSwitch.PutToRightOf(terrainSelector, terrainPhysicsToggleSwitch.GetWidth(), Offset.Right(-200f));
            var towerDesignationsToggleSwitch = CreateTerrainIgnoreMineTowerDesignationsToggleSwitch();
            towerDesignationsToggleSwitch.PutToRightOf(terrainPhysicsToggleSwitch, towerDesignationsToggleSwitch.GetWidth(), Offset.Right(-250f));


            var instantTerrainActions = Builder.NewPanel("instantTerrainActions").SetBackground(Builder.Style.Panel.ItemOverlay);
            instantTerrainActions.AppendTo(tabContainer, size: 50f, Offset.All(0));

            var instantTerrainButtonContainer = Builder
                .NewStackContainer("instantTerrainButtonContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .PutToLeftOf(instantTerrainActions, 0.0f, Offset.Left(10f));

            var buildMineButton = BuildMineButton();
            buildMineButton.AppendTo(instantTerrainButtonContainer, buildMineButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);

            var buildDumpButton = BuildDumpButton();
            buildDumpButton.AppendTo(instantTerrainButtonContainer, buildDumpButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);

            var buildChangeTerrainButton = BuildChangeTerrainButton();
            buildChangeTerrainButton.AppendTo(instantTerrainButtonContainer, buildChangeTerrainButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            

            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("其他"));

            var otherTerrainActions = Builder.NewPanel("instantTerrainActions").SetBackground(Builder.Style.Panel.ItemOverlay);
            otherTerrainActions.AppendTo(tabContainer, size: 50f, Offset.All(0));
            var otherTerrainButtonContainer = Builder
                .NewStackContainer("otherTerrainButtonContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .PutToLeftOf(otherTerrainActions, 0.0f, Offset.Left(10f));

            var buildRefillGroundWaterButton = BuildRefillGroundWaterButton();
            buildRefillGroundWaterButton.AppendTo(otherTerrainButtonContainer, buildRefillGroundWaterButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);

            var buildRefillGroundCrudeButton = BuildRefillGroundCrudeButton();
            buildRefillGroundCrudeButton.AppendTo(otherTerrainButtonContainer, buildRefillGroundCrudeButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);

            var buildAddTreesButton = BuildAddsTreesButton();
            buildAddTreesButton.AppendTo(otherTerrainButtonContainer, buildAddTreesButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            var buildRemoveTreesButton = BuildRemoveTreesButton();
            buildRemoveTreesButton.AppendTo(otherTerrainButtonContainer, buildRemoveTreesButton.GetOptimalSize(), ContainerPosition.MiddleOrCenter);


        }

        private Dropdwn BuildTerrainSelector(StackContainer topOf)
        {
            var productDropdown = Builder
                .NewDropdown("TerrainDumpSelector")
                .AddOptions(_looseProductProtos.Select(x => x.Id.ToString().Replace("Product_", "")).ToList())
                .OnValueChange(i => _selectedLooseProductProto = (ProductProto.ID)_looseProductProtos.ElementAt(i)?.Id);

            _selectedLooseProductProto = _looseProductProtos.ElementAt(0)?.Id;

            return productDropdown;
        }
        
        private SwitchBtn CreateTerrainIgnoreMineTowerDesignationsToggleSwitch()
        {
            var toggleBtn = Builder.NewSwitchBtn()
                .SetText("矿场控制塔")
                .AddTooltip("当立即完成采矿或倾倒作业时，矿场控制塔下的指定.")
                .SetOnToggleAction((toggleVal) => _ignoreMineTowerDesignations = toggleVal);

            _switchBtns.Add(toggleBtn, () => _ignoreMineTowerDesignations);

            return toggleBtn;
        }
        
        private SwitchBtn CreateTerrainPhysicsToggleSwitch()
        {
            var toggleBtn = Builder.NewSwitchBtn()
                .SetText("禁用地形物理")
                .AddTooltip(
                    "当立即完成采矿或倾倒指定时，此切换将指示游戏物理引擎是否会影响修改后的地形。启用后，您所做的任何地形修改都会出现非常锐利的边缘。注意：车辆在无物理地形附近采矿/倾倒可能会导致无物理地形开始对物理做出响应.")
                .SetOnToggleAction((toggleVal) => _disableTerrainPhysicsOnMiningAndDumping = toggleVal);


            _switchBtns.Add(toggleBtn, () => _disableTerrainPhysicsOnMiningAndDumping);

            return toggleBtn;
        }

        private StackContainer CreateStackContainer()
        {
            var topOf = Builder
                .NewStackContainer("container")
                .SetStackingDirection(StackContainer.Direction.TopToBottom)
                .SetSizeMode(StackContainer.SizeMode.Dynamic)
                .SetInnerPadding(Offset.All(15f))
                .SetItemSpacing(5f)
                .PutToTopOf(this, 0.0f);
            return topOf;
        }

        private Btn BuildMineButton()
        {
            var btn = Builder.NewBtnPrimary("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("矿产立即挖掘完毕"))
                .AddToolTip(
                    "目前指定用于采矿的所有区域都将立即完成采矿作业。这将导致玩家没有资源.")
                .OnClick(() => _cheatProvider.CompleteAllMiningDesignations(_ignoreMineTowerDesignations, _disableTerrainPhysicsOnMiningAndDumping));

            return btn;
        }

        private Btn BuildDumpButton()
        {
            var btn = Builder.NewBtnPrimary("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("立即完成倾倒"))
                .AddToolTip(
                    "目前指定倾倒的所有区域将立即完成倾倒作业。不需要玩家提供任何资源.")
                .OnClick(() => _cheatProvider.CompleteAllDumpingDesignationsWithProduct((ProductProto.ID)_selectedLooseProductProto, _disableTerrainPhysicsOnMiningAndDumping, _ignoreMineTowerDesignations));

            return btn;
        }
        
        private Btn BuildChangeTerrainButton()
        {
            var btn = Builder.NewBtnPrimary("button")
                    .SetButtonStyle(Style.Global.PrimaryBtn)
                    .SetText(new LocStrFormatted("指定倾倒"))
                    .AddToolTip(
                        "当前指定用于倾倒的所有区域都将用作更改地形下拉列表中所选地形的标记。地形的高度不会改变，只会改变最上面1层的材质。可用于为农场开辟泥土.")
                .OnClick(() => _cheatProvider.ChangeTerrain((ProductProto.ID)_selectedLooseProductProto, _ignoreMineTowerDesignations));
                

            return btn;
        }

        private Btn BuildRemoveTreesButton()
        {
            var btn = Builder.NewBtnPrimary("button")
                .SetButtonStyle(Style.Global.DangerBtn)
                .SetText("移除树木")
                .AddToolTip("立即清除指定由收割机清除的所有树木。导致玩家没有资源.")
                .OnClick(() => _cheatProvider.RemoveAllSelectedTrees());

            return btn;
        }
        
        private Btn BuildAddsTreesButton()
        {
            var btn = Builder.NewBtnPrimary("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText("添加树木")
                .AddToolTip("目前指定用于倾倒且不受采矿塔控制的所有区域都将用作植树地点的标记。树木种植间距适中.")
                .OnClick(() => _cheatProvider.AddTreesToDumpingDesignations());

            return btn;
        }

        private Btn BuildRefillGroundWaterButton()
        {
            var btn = Builder.NewBtnPrimary("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("填充地下水"))
                .AddToolTip("所有地面储备水将被补充至满容量")
                .OnClick(() => _cheatProvider.RefillGroundWaterReserve());

            return btn;
        }

        private Btn BuildRefillGroundCrudeButton()
        {
            var btn = Builder.NewBtnPrimary("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("填充原油"))
                .AddToolTip("所有地面原油储备将满负荷补充")
                .OnClick(() => _cheatProvider.RefillGroundCrudeReserve());

            return btn;
        }

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
    }
}