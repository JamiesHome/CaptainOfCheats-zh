using System;
using System.Collections.Generic;
using System.Linq;
using Mafi;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Syncers;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using Mafi.Unity.UiFramework;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UiFramework.Components.Tabs;
using Mafi.Unity.UserInterface.Components;
using UnityEngine;

namespace CaptainOfCheats.Cheats.Shipyard
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class ShipyardCheatTab : Tab, ICheatProviderTab
    {
        private readonly ShipyardCheatProvider _shipyardCheatProvider;
        private readonly IEnumerable<ProductProto> _productProtos;
        private readonly FleetCheatProvider _fleetCheatProvider;
        private readonly ProtosDb _protosDb;
        private float _quantity = 250;
        private ProductProto.ID? _selectedProduct;

        public ShipyardCheatTab(NewInstanceOf<ShipyardCheatProvider> productCheatProvider, NewInstanceOf<FleetCheatProvider> fleetCheatProvider, ProtosDb protosDb) : base(nameof(ShipyardCheatTab),
            SyncFrequency.OncePerSec)
        {
            _shipyardCheatProvider = productCheatProvider.Instance;
            _fleetCheatProvider = fleetCheatProvider.Instance;
            _protosDb = protosDb;
            _productProtos = _protosDb.Filter<ProductProto>(proto => proto.CanBeLoadedOnTruck).OrderBy(x => x);
        }

        public string Name => "船舶";
        public string IconPath => Assets.Unity.UserInterface.Toolbar.CargoShip_svg;

        protected override void BuildUi()
        {
            var tabContainer = CreateStackContainer();
            
            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("船只码头产品储存"), new LocStrFormatted("添加或删除 Shipyard 仓库中的产品."), Offset.Zero);
            var sectionTitlesContainer = Builder
                .NewStackContainer("shipyardContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .AppendTo(tabContainer, offset: Offset.All(0), size: 30);

            var quantitySectionTitle = Builder.CreateSectionTitle(new LocStrFormatted("操作数量"), new LocStrFormatted("设置将受添加或删除产品操作影响的产品数量."));
            quantitySectionTitle.AppendTo(sectionTitlesContainer,  quantitySectionTitle.GetPreferedWidth(), Mafi.Unity.UiFramework.Offset.Left(10));
            
            var productSectionTitle = Builder.CreateSectionTitle(new LocStrFormatted("产品"), new LocStrFormatted("选择要从您的船舶码头添加/删除的产品."));
            productSectionTitle.AppendTo(sectionTitlesContainer, productSectionTitle.GetPreferedWidth(), Offset.Left(245));
            
            var quantityAndProductContainer = Builder
                .NewStackContainer("quantityAndProductContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .AppendTo(tabContainer, offset: Offset.Left(10), size: 30);
            
            var quantitySlider = BuildQuantitySlider();
            quantitySlider.AppendTo(quantityAndProductContainer, new Vector2(200, 28f), ContainerPosition.LeftOrTop);
            
            var buildProductSelector = BuildProductSelector();
            buildProductSelector.AppendTo(quantityAndProductContainer, new Vector2(200, 28f), ContainerPosition.LeftOrTop, Offset.Left(100));

            var thirdRowContainer = Builder
                .NewStackContainer("secondRowContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .AppendTo(tabContainer,offset: Offset.Left(10), size: 30);

            var spawnProductBtn = BuildAddProductBtn();
            spawnProductBtn.AppendTo(thirdRowContainer, spawnProductBtn.GetOptimalSize(), ContainerPosition.LeftOrTop, Offset.Top(10f));
            
            Panel horSep = this.Builder.NewPanel("separator").AppendTo<Panel>(tabContainer, new Vector2?(new Vector2(630f, 20f)), ContainerPosition.MiddleOrCenter, Offset.Top(20));
            this.Builder.NewIconContainer("left").SetIcon("Assets/Unity/UserInterface/General/HorizontalGradientToLeft48.png", false).PutToLeftMiddleOf<IconContainer>((IUiElement) horSep, new Vector2(300f, 1f));
            this.Builder.NewIconContainer("symbol").SetIcon("Assets/Unity/UserInterface/General/Tradable128.png").PutToCenterMiddleOf<IconContainer>((IUiElement) horSep, new Vector2(20f, 20f));
            this.Builder.NewIconContainer("right").SetIcon("Assets/Unity/UserInterface/General/HorizontalGradientToRight48.png", false).PutToRightMiddleOf<IconContainer>((IUiElement) horSep, new Vector2(300f, 1f));
            
            Builder.AddSectionTitle(tabContainer, new LocStrFormatted("舰船探索"));
            var mainShipPanel = Builder.NewPanel("mainShipPanel").SetBackground(Builder.Style.Panel.ItemOverlay);
            mainShipPanel.AppendTo(tabContainer, size: 50f, Offset.All(0));

            var mainShipBtnContainer = Builder
                .NewStackContainer("mainShipBtnContainer")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetSizeMode(StackContainer.SizeMode.StaticDirectionAligned)
                .SetItemSpacing(10f)
                .PutToLeftOf(mainShipPanel, 0.0f, Offset.Left(10f));
            
            var forceUnloadShipBtn = BuildForceUnloadShipyardShipButton();
            forceUnloadShipBtn.AppendTo(mainShipBtnContainer, forceUnloadShipBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            var finishExplorationBtn = BuildFinishExplorationButton();
            finishExplorationBtn.AppendTo(mainShipBtnContainer, finishExplorationBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
            
            var repairShipBtn = BuildRepairFleetButton();
            repairShipBtn.AppendTo(mainShipBtnContainer, repairShipBtn.GetOptimalSize(), ContainerPosition.MiddleOrCenter);
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

        private Btn BuildAddProductBtn()
        {
            var btn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("添加产品"))
                .AddToolTip("将所选数量的产品添加到您的船舶码头仓库中.")
                .OnClick(() => _shipyardCheatProvider.AddItemToShipyard(_selectedProduct.Value, (int)_quantity));

            return btn;
            
        }

        private Btn BuildFinishExplorationButton()
        {
            var btn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("完成探索"))
                .AddToolTip("让你的船执行一个探索，然后按下这个按钮，他们会立即探索完成.")
                .OnClick(() => _fleetCheatProvider.FinishExploration());

            return btn;
        }

        private Btn BuildRepairFleetButton()
        {
            var btn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText(new LocStrFormatted("立即修理船舶"))
                .AddToolTip("将你的舰船修复至完全健康.")
                .OnClick(() => _fleetCheatProvider.RepairFleet());

            return btn;
        }
        
        private Btn BuildForceUnloadShipyardShipButton()
        {
            var btn = Builder.NewBtnGeneral("button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText("强行卸载船舶货物")
                .AddToolTip("绕过造船厂货物容量检查并将您的船强行卸载到您的造船厂货物中。")
                .OnClick(() => _shipyardCheatProvider.ForceUnloadShipyardShip());
            return btn;
        }

        private Dropdwn BuildProductSelector()
        {
            var productDropdown = Builder
                .NewDropdown("ProductDropDown")
                .AddOptions(_productProtos.Select(x => x.Id.ToString().Replace("Product_", "")).ToList())
                .OnValueChange(i => _selectedProduct = _productProtos.ElementAt(i)?.Id);

            _selectedProduct = _productProtos.ElementAt(0)?.Id;

            return productDropdown;
        }

        private Slidder BuildQuantitySlider()
        {
            var sliderLabel = Builder
                .NewTxt("")
                .SetTextStyle(Builder.Style.Global.TextControls)
                .SetAlignment(TextAnchor.MiddleLeft);
            var qtySlider = Builder
                .NewSlider("qtySlider")
                .SimpleSlider(Builder.Style.Panel.Slider)
                .SetValuesRange(10f, 10000f)
                .OnValueChange(
                    qty => { sliderLabel.SetText(Math.Round(qty).ToString()); },
                    qty =>
                    {
                        sliderLabel.SetText(Math.Round(qty).ToString());
                        _quantity = qty;
                    })
                .SetValue(_quantity);


            sliderLabel.PutToRightOf(qtySlider, 90f, Offset.Right(-110f));

            return qtySlider;
        }
    }
}