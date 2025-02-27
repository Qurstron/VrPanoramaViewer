using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using JSONClasses;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class LabelComponent : WorldSelectable
{
    public override string Name { get => label.name; set => label.name = value; }

    public class LabelAnglePoint : AnglePoint
    {
        public LabelComponent labelComponent;
        public override Vector2 Angle
        {
            get { return labelComponent.Coord; }
            set { labelComponent.Coord = value; }
        }
        public override Vector2 JsonAngle
        {
            get
            {
                return labelComponent.JsonCoord;// new(labelComponent.label.pos[0], labelComponent.label.pos[1]);
            }
            set
            {
                //labelComponent.label.pos[0] = value.x;
                //labelComponent.label.pos[1] = value.y;
                //labelComponent.Coord = value;
                labelComponent.JsonCoord = value;
            }
        }

        public LabelAnglePoint(LabelComponent labelComponent)
        {
            this.labelComponent = labelComponent;
            relatedComponent = labelComponent;
        }
    }

    private Vector2 coord;
    private Label label;
    public Label Label
    {
        get { return label; }
        set
        {
            label = value;
            Subject = value;

            Header = label.header;
            Content = label.content;
            Coord = new(label.pos[0], label.pos[1]);

            points.Clear();
            points.Add(new LabelAnglePoint(this));
        }
    }

    public string JsonHeader
    {
        get
        {
            return label.header;
        }
        set
        {
            label.header = value;
            Header = value;
        }
    }
    public string JsonContent
    {
        get
        {
            return label.content;
        }
        set
        {
            label.content = value;
            Content = value;
        }
    }
    public Vector2 JsonCoord
    {
        get
        {
            return new(Label.pos[0], Label.pos[1]);
        }
        set
        {
            Label.pos[0] = value.x;
            Label.pos[1] = value.y;
            Coord = value;
        }
    }
    public string Header
    {
        set
        {
            Transform text = transform.Find("Canvas/Text/Header");
            text.GetComponent<TMP_Text>().text = value;
            LayoutRebuilder.ForceRebuildLayoutImmediate(text as RectTransform);
        }
    }
    public string Content
    {
        set
        {
            Transform text = transform.Find("Canvas/Text/Content");
            text.GetComponent<TMP_Text>().text = value;
            LayoutRebuilder.ForceRebuildLayoutImmediate(text as RectTransform);
        }
    }
    public Vector2 Coord
    {
        get { return coord; }
        set
        {
            coord = value;
            transform.localPosition = PanoramaSphereController.convertArrayToPos(value);
        }
    }

    public override void Remove()
    {
        GetOrigin().labels.Remove(Label);
    }
    public override void Add()
    {
        GetOrigin().labels.Add(Label);
    }

    public override NodeContent GetOrigin()
    {
        return Label.origin;
    }
}
