using GLTFast;
using GLTFast.Logging;
using JSONClasses;
using System.Linq;
using UnityEngine;

public class CustomGameObjectInstantiator : GameObjectInstantiator
{
    private PanoramaSphereController panoramaSphereController;
    private Object3D object3D;

    public CustomGameObjectInstantiator(IGltfReadable gltf, Transform parent, PanoramaSphereController panoramaSphereController, Object3D object3D, ICodeLogger logger = null, InstantiationSettings settings = null) : base(gltf, parent, logger, settings)
    {
        this.panoramaSphereController = panoramaSphereController;
        this.object3D = object3D;
    }
    public override void AddPrimitive(uint nodeIndex, string meshName, MeshResult meshResult, uint[] joints = null, uint? rootJoint = null, float[] morphTargetWeights = null, int primitiveNumeration = 0)
    {
        base.AddPrimitive(nodeIndex, meshName, meshResult, joints, rootJoint, morphTargetWeights, primitiveNumeration);
        
        if (meshResult.mesh.vertices.Distinct().Count() >= 3)
        {
            GameObject node = m_Nodes[nodeIndex];
            var container = QUtils.GetOrAddComponent<ObjectSelectableContainer>(node);//.objectSelectable = objectSelectable;
            QUtils.GetOrAddComponent<MeshCollider>(node).convex = true;
            container.selectable = new()
            {
                panoramaSphereController = panoramaSphereController,
                gameObject = node,
                object3D = object3D,
            };
        }
    }
}
