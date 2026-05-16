using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;

public class BossStateMachine : StateMachine<BossStateType, IBossFSMObj>
{
    public BossStateMachine(IBossFSMObj aiObj) : base(aiObj)
    {
    }
}