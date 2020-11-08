using System.Linq;
using Naninovel;
using Naninovel.Commands;
using UnityEngine;

namespace OVDEN.Helper
{
    public class Crop
    {
        private readonly Vector2 referenceResolution;
        private readonly Vector2 size;
        private readonly Vector2 position;
        
        private readonly CameraConfiguration cameraConfiguration;
        
        public Crop(DecimalListParameter parameter) : this(
            new Vector2(
                parameter?.ElementAtOrDefault(0) ?? 0.0f,
                parameter?.ElementAtOrDefault(1) ?? 0.0f),
            new Vector2(
                parameter?.ElementAtOrDefault(2) ?? 0.0f,
                parameter?.ElementAtOrDefault(3) ?? 0.0f)) { }

        public Crop(Vector2 position, Vector2 size)
        {
            cameraConfiguration = Engine.GetConfiguration<CameraConfiguration>();
            referenceResolution = cameraConfiguration.ReferenceResolution;
            this.position = position;
            this.size = size == Vector2.zero ? referenceResolution : size;
        }

        public float?[] AttemptScaleFloat()
        {
            Vector3 scale = AttemptScaleVector();
            float?[] result = new float?[3];
            result[0] = scale.x;
            result[1] = scale.y;
            result[2] = scale.z;
            return result;
        }

        public Vector3 AttemptScaleVector()
        {
            return new Vector3
            {
                [0] = referenceResolution.x / size.x,
                [1] = referenceResolution.y / size.y,
                [2] = 1.0f
            };
        }

        public float?[] AttemptPositionFloat()
        {
            Vector3 positionVector = AttemptPositionVector();
            float?[] result = new float?[3];
            result[0] = positionVector.x;
            result[1] = positionVector.y;
            result[2] = positionVector.z;
            return result;
        }

        public Vector3 AttemptPositionVector()
        {
            Vector3 scale = AttemptScaleVector();
            Vector2 referenceSize = cameraConfiguration.ReferenceSize;
            
            float offsetCenterX = (referenceResolution.x * scale.x) / 2;
            float offsetCenterX1 = referenceResolution.x * scale.x - ((size.x * scale.x) / 2);
            
            float offsetX =   offsetCenterX -offsetCenterX1;
            
            float offsetCenterY = (referenceResolution.y * scale.y) / 2;
            float offsetCenterY1 = referenceResolution.y * scale.y - ((size.y * scale.y) / 2);
            
            float offsetY = offsetCenterY1 - offsetCenterY;

            float centerX = (position.x * scale.x) + ((size.x * scale.x) / 2) + offsetX;
            float centerY = (referenceResolution.y * scale.y - (position.y * scale.y)) - ((size.y * scale.y)/ 2.0f) - offsetY;
            
            float x = 1.0f - (centerX / referenceResolution.x);
            float y = centerY / referenceResolution.y;
            
            Vector2 originPosition = -referenceSize / 2f;
            
            return new Vector3
            {
                [0] = (originPosition + Vector2.Scale(new Vector2(x,0.0f), referenceSize)).x,
                [1] = -(originPosition + Vector2.Scale(new Vector2(0.0f,y), referenceSize)).y,
                [2] = 100
            };
        }
    }
}