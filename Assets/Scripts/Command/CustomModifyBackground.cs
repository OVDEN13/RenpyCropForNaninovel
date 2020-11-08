using OVDEN.Helper;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    [CommandAlias("cropBack")]
    public class CustomModifyBackground : ModifyBackground
    {
        [ParameterAlias("crop")]
        public DecimalListParameter СropParameter;

        private Crop crop;

        protected override float?[] AssignedPosition => crop.AttemptPositionFloat();
        protected override float?[] AssignedScale => crop.AttemptScaleFloat();

        protected override async UniTask ApplyModificationsAsync(IBackgroundActor actor, EasingType easingType,CancellationToken cancellationToken)
        {
            crop = new Crop(СropParameter);
            await base.ApplyModificationsAsync(actor, easingType, cancellationToken);
        }
    }
}