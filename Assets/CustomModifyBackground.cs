using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    [CommandAlias("custom_back")]
    public class CustomModifyBackground : ModifyBackground
    {
        
        [ParameterAlias("size")]
        public DecimalListParameter SizeParameter;
        [ParameterAlias("crop")]
        public DecimalListParameter СropParameter;
        

        protected override float?[] AssignedPosition => AttemptPosition();
        protected override float?[] AssignedScale => AttemptScale();
        
        struct CustomBackData
        {
            public Vector2 originSize;
            public Vector2 size;
            public Vector2 position;
        }
        
        private CustomBackData customBackData;
        
        protected override async UniTask ApplyModificationsAsync(IBackgroundActor actor, EasingType easingType,CancellationToken cancellationToken)
        {
            initData();
            base.ApplyModificationsAsync(actor, easingType, cancellationToken);
        }

        private void initData()
        {
            Vector2 referenceSize = CameraManager.Configuration.ReferenceSize;
            
            customBackData.originSize = new Vector2
            {
                [0] = SizeParameter?.ElementAtOrDefault(0) ?? referenceSize.x,
                [1] = SizeParameter?.ElementAtOrDefault(1) ?? referenceSize.y
            };
            customBackData.position = new Vector2
            {
                [0] = СropParameter?.ElementAtOrDefault(0) ?? 0.0f,
                [1] = СropParameter?.ElementAtOrDefault(1) ?? 0.0f
            };
            customBackData.size = new Vector2
            {
                [0] = СropParameter?.ElementAtOrDefault(2) ?? referenceSize.x,
                [1] = СropParameter?.ElementAtOrDefault(3) ?? referenceSize.y
            };
        }

        private float?[] AttemptScale ()
        {
            float?[] result = new float?[3];
            result[0] = customBackData.originSize.x / customBackData.size.x;
            result[1] = customBackData.originSize.y / customBackData.size.y;
            result[2] = 1.0f;
            return result;
        }

        private float?[] AttemptPosition()
        {
            Vector2 referenceSize = CameraManager.Configuration.ReferenceSize;
            float?[] c = AttemptScale();
            // ReSharper disable once PossibleInvalidOperationException
            Vector2 scale = new Vector2((float)c[0],(float)c[1]);
            float offsetCenterX = (customBackData.originSize.x * scale.x) / 2;
            float offsetCenterX1 = customBackData.originSize.x - (customBackData.size.x / 2);
            float offsetX = offsetCenterX1 - offsetCenterX;
            
            float offsetCenterY = (customBackData.originSize.y * scale.y) / 2;
            float offsetCenterY1 = customBackData.originSize.y - (customBackData.size.y / 2);
            float offsetY = offsetCenterY1 - offsetCenterY;

            float centerX = customBackData.position.x + (customBackData.size.x / 2) + offsetX;
            float centerY = (customBackData.originSize.y - customBackData.position.y) - (customBackData.size.y / 2.0f) - offsetY;
            
            float x = 1.0f - (centerX / customBackData.originSize.x);
            float y = centerY / customBackData.originSize.y;
            
            Vector2 originPosition = -referenceSize / 2f;
            
            float?[] result = new float?[3];
            result[0] = (originPosition + Vector2.Scale(new Vector2(x, 0.0f), referenceSize)).x ;
            result[1] = -(originPosition + Vector2.Scale(new Vector2(0.0f,y), referenceSize)).y ;
            result[2] = 100;
            return result;
        }
    }
}