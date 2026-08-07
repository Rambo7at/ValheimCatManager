using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimCatManager.Config;

public class AttackSpeedConfig
{
    public float PrimarySpeed { get; set; }  
    public float SecondarySpeed { get; set; }

    public AttackSpeedConfig(float deltaPrimary, float deltaSecondary)
    {
        PrimarySpeed = deltaPrimary;
        SecondarySpeed = deltaSecondary;
    }
}

