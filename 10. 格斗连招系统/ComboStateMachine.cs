namespace ComboSystemStateMachine;

/// <summary>
/// 连招系统状态机
/// </summary>
public class ComboStateMachine
{
    private readonly Dictionary<string, ComboNode> _allMoves = new();
    private readonly InputBuffer _inputBuffer;
    private readonly MotionDetector _motionDetector;
    
    private ComboNode? _currentNode;
    private int _currentAttackFrame;
    private CharacterState _state;
    private int _comboCount;
    private int _comboDamage;
    private float _damageScaling = 1.0f;
    private float _meter;
    private const float MaxMeter = 100f;
    
    public CharacterState State => _state;
    public int ComboCount => _comboCount;
    public int ComboDamage => _comboDamage;
    public float Meter => _meter;
    public string? CurrentMoveName => _currentNode?.Attack.Name;
    
    public event Action<AttackData, int>? OnAttackExecuted;
    public event Action<int, int>? OnComboEnded;
    public event Action<string>? OnSpecialMoveExecuted;
    
    public ComboStateMachine()
    {
        _inputBuffer = new InputBuffer();
        _motionDetector = new MotionDetector(_inputBuffer);
        _state = CharacterState.Idle;
        
        InitializeMoveList();
    }
    
    private void InitializeMoveList()
    {
        // 基本攻击
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "立轻拳",
                Type = AttackType.Light,
                Damage = 30,
                StartupFrames = 4,
                ActiveFrames = 3,
                RecoveryFrames = 8,
                HitStun = 12,
                MeterGain = 5
            },
            IsStarter = true,
            CancelWindowStart = 4,
            CancelWindowEnd = 10
        });
        
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "立轻脚",
                Type = AttackType.Light,
                Damage = 35,
                StartupFrames = 5,
                ActiveFrames = 3,
                RecoveryFrames = 10,
                HitStun = 14,
                MeterGain = 5
            },
            IsStarter = true,
            CancelWindowStart = 5,
            CancelWindowEnd = 12
        });
        
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "立重拳",
                Type = AttackType.Medium,
                Damage = 80,
                StartupFrames = 8,
                ActiveFrames = 4,
                RecoveryFrames = 18,
                HitStun = 20,
                MeterGain = 10
            },
            CancelWindowStart = 8,
            CancelWindowEnd = 18
        });
        
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "立重脚",
                Type = AttackType.Heavy,
                Damage = 100,
                StartupFrames = 12,
                ActiveFrames = 5,
                RecoveryFrames = 22,
                HitStun = 24,
                Launcher = true,
                MeterGain = 15
            },
            CancelWindowStart = 12,
            CancelWindowEnd = 22
        });
        
        // 蹲姿攻击
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "蹲轻拳",
                Type = AttackType.Light,
                Damage = 25,
                StartupFrames = 3,
                ActiveFrames = 2,
                RecoveryFrames = 7,
                HitStun = 10,
                MeterGain = 4
            },
            IsStarter = true,
            CancelWindowStart = 3,
            CancelWindowEnd = 8
        });
        
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "蹲重脚",
                Type = AttackType.Heavy,
                Damage = 90,
                StartupFrames = 10,
                ActiveFrames = 4,
                RecoveryFrames = 20,
                HitStun = 0,
                Animation = "sweep",
                MeterGain = 12
            },
            IsEnder = true
        });
        
        // 必杀技
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "波动拳",
                Type = AttackType.Special,
                Damage = 70,
                StartupFrames = 12,
                ActiveFrames = 10,
                RecoveryFrames = 30,
                HitStun = 18,
                MeterGain = 20,
                Animation = "hadouken"
            },
            IsEnder = true
        });
        
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "升龙拳",
                Type = AttackType.Special,
                Damage = 120,
                StartupFrames = 3,
                ActiveFrames = 12,
                RecoveryFrames = 35,
                HitStun = 0,
                Launcher = true,
                MeterGain = 25,
                Animation = "shoryuken"
            },
            IsEnder = true
        });
        
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "旋风腿",
                Type = AttackType.Special,
                Damage = 100,
                StartupFrames = 8,
                ActiveFrames = 20,
                RecoveryFrames = 25,
                HitStun = 15,
                MeterGain = 18,
                Animation = "tatsumaki"
            }
        });
        
        // 超必杀
        AddMove(new ComboNode
        {
            Attack = new AttackData
            {
                Name = "真空波动拳",
                Type = AttackType.Super,
                Damage = 300,
                StartupFrames = 8,
                ActiveFrames = 25,
                RecoveryFrames = 40,
                HitStun = 30,
                MeterGain = 0,
                Animation = "super_hadouken"
            },
            IsEnder = true
        });
        
        // 设置连招路径
        SetupComboRoutes();
    }
    
    private void AddMove(ComboNode node)
    {
        _allMoves[node.Attack.Name] = node;
    }
    
    private void SetupComboRoutes()
    {
        // 轻拳 → 轻拳/轻脚/重拳
        _allMoves["立轻拳"].Links.Add(new ComboLink
        {
            RequiredInput = new[] { InputButton.LightPunch },
            TargetNode = _allMoves["立轻拳"],
            Type = LinkType.Chain,
            LinkWindow = 12
        });
        
        _allMoves["立轻拳"].Links.Add(new ComboLink
        {
            RequiredInput = new[] { InputButton.LightKick },
            TargetNode = _allMoves["立轻脚"],
            Type = LinkType.Chain,
            LinkWindow = 12
        });
        
        _allMoves["立轻拳"].Links.Add(new ComboLink
        {
            RequiredInput = new[] { InputButton.HeavyPunch },
            TargetNode = _allMoves["立重拳"],
            Type = LinkType.Chain,
            LinkWindow = 15
        });
        
        // 轻脚 → 重拳/重脚
        _allMoves["立轻脚"].Links.Add(new ComboLink
        {
            RequiredInput = new[] { InputButton.HeavyPunch },
            TargetNode = _allMoves["立重拳"],
            Type = LinkType.Chain,
            LinkWindow = 15
        });
        
        _allMoves["立轻脚"].Links.Add(new ComboLink
        {
            RequiredInput = new[] { InputButton.HeavyKick },
            TargetNode = _allMoves["立重脚"],
            Type = LinkType.Chain,
            LinkWindow = 15
        });
        
        // 重拳 → 必杀技取消
        _allMoves["立重拳"].Links.Add(new ComboLink
        {
            RequiredInput = MotionDetector.QuarterCircleForward.Append(InputButton.LightPunch).ToArray(),
            TargetNode = _allMoves["波动拳"],
            Type = LinkType.Cancel,
            LinkWindow = 20
        });
        
        _allMoves["立重拳"].Links.Add(new ComboLink
        {
            RequiredInput = MotionDetector.DragonPunch.Append(InputButton.LightPunch).ToArray(),
            TargetNode = _allMoves["升龙拳"],
            Type = LinkType.Cancel,
            LinkWindow = 20
        });
        
        // 重脚（浮空）→ 超必杀取消
        _allMoves["立重脚"].Links.Add(new ComboLink
        {
            RequiredInput = MotionDetector.QuarterCircleForward
                .Concat(MotionDetector.QuarterCircleForward)
                .Append(InputButton.LightPunch).ToArray(),
            TargetNode = _allMoves["真空波动拳"],
            Type = LinkType.SuperCancel,
            LinkWindow = 25,
            RequiresMeter = true,
            MeterCost = 50
        });
    }
    
    /// <summary>
    /// 处理输入
    /// </summary>
    public void ProcessInput(InputButton button)
    {
        _inputBuffer.AddInput(button);
        
        Console.WriteLine($"  [输入] {FormatButton(button)} (帧: {_inputBuffer.CurrentFrame})");
        
        // 检查特殊指令
        if (_state == CharacterState.Idle || _state == CharacterState.Attacking)
        {
            if (TryExecuteSpecialMove()) return;
        }
        
        // 检查普通攻击/连招
        if (_state == CharacterState.Idle)
        {
            TryStartCombo(button);
        }
        else if (_state == CharacterState.Attacking && _currentNode != null)
        {
            TryContinueCombo(button);
        }
    }
    
    private bool TryExecuteSpecialMove()
    {
        // 超必杀检测 (↓↘→↓↘→ + P)
        if (_meter >= 50 && _inputBuffer.CheckSequence(
            MotionDetector.QuarterCircleForward
                .Concat(MotionDetector.QuarterCircleForward)
                .Append(InputButton.LightPunch).ToArray()))
        {
            ExecuteMove(_allMoves["真空波动拳"]);
            _meter -= 50;
            OnSpecialMoveExecuted?.Invoke("真空波动拳");
            return true;
        }
        
        // 升龙拳检测 (→↓↘ + P)
        if (_motionDetector.DetectShoryuken(InputButton.LightPunch) ||
            _motionDetector.DetectShoryuken(InputButton.HeavyPunch))
        {
            ExecuteMove(_allMoves["升龙拳"]);
            OnSpecialMoveExecuted?.Invoke("升龙拳");
            return true;
        }
        
        // 波动拳检测 (↓↘→ + P)
        if (_motionDetector.DetectHadouken(InputButton.LightPunch) ||
            _motionDetector.DetectHadouken(InputButton.HeavyPunch))
        {
            ExecuteMove(_allMoves["波动拳"]);
            OnSpecialMoveExecuted?.Invoke("波动拳");
            return true;
        }
        
        // 旋风腿检测 (↓↙← + K)
        if (_motionDetector.DetectTatsumaki(InputButton.LightKick) ||
            _motionDetector.DetectTatsumaki(InputButton.HeavyKick))
        {
            ExecuteMove(_allMoves["旋风腿"]);
            OnSpecialMoveExecuted?.Invoke("旋风腿");
            return true;
        }
        
        return false;
    }
    
    private void TryStartCombo(InputButton button)
    {
        ComboNode? starter = null;
        
        if (button.HasFlag(InputButton.LightPunch))
        {
            starter = _allMoves["立轻拳"];
        }
        else if (button.HasFlag(InputButton.LightKick))
        {
            starter = _allMoves["立轻脚"];
        }
        else if (button.HasFlag(InputButton.Down) && button.HasFlag(InputButton.LightPunch))
        {
            starter = _allMoves["蹲轻拳"];
        }
        else if (button.HasFlag(InputButton.HeavyPunch))
        {
            starter = _allMoves["立重拳"];
        }
        else if (button.HasFlag(InputButton.HeavyKick))
        {
            starter = _allMoves["立重脚"];
        }
        
        if (starter != null)
        {
            StartCombo();
            ExecuteMove(starter);
        }
    }
    
    private void TryContinueCombo(InputButton button)
    {
        if (_currentNode == null) return;
        
        // 检查是否在取消窗口内
        if (!_currentNode.CanCancel(_currentAttackFrame))
        {
            Console.WriteLine($"  [取消失败] 不在取消窗口内 (当前帧: {_currentAttackFrame}, 窗口: {_currentNode.CancelWindowStart}-{_currentNode.CancelWindowEnd})");
            return;
        }
        
        // 查找可用的链接
        foreach (var link in _currentNode.Links)
        {
            // 检查气量需求
            if (link.RequiresMeter && _meter < link.MeterCost)
                continue;
            
            // 检查输入匹配
            bool matches = false;
            if (link.RequiredInput.Length == 1)
            {
                matches = button.HasFlag(link.RequiredInput[0]);
            }
            else
            {
                matches = _inputBuffer.CheckSequence(link.RequiredInput, link.LinkWindow);
            }
            
            if (matches)
            {
                if (link.RequiresMeter)
                {
                    _meter -= link.MeterCost;
                }
                
                Console.WriteLine($"  [连招] {link.Type} → {link.TargetNode.Attack.Name}");
                ExecuteMove(link.TargetNode);
                return;
            }
        }
    }
    
    private void ExecuteMove(ComboNode node)
    {
        _currentNode = node;
        _currentAttackFrame = 0;
        _state = node.Attack.Type == AttackType.Super ? CharacterState.SuperMove :
                 node.Attack.Type == AttackType.Special ? CharacterState.SpecialMove :
                 CharacterState.Attacking;
        
        // 计算伤害缩放
        var scaledDamage = (int)(node.Attack.Damage * _damageScaling);
        _comboDamage += scaledDamage;
        _comboCount++;
        
        // 增加气量
        _meter = Math.Min(MaxMeter, _meter + node.Attack.MeterGain);
        
        // 更新伤害缩放
        _damageScaling = Math.Max(0.1f, _damageScaling - 0.1f);
        
        Console.WriteLine($"\n  ═══════════════════════════════════════════");
        Console.WriteLine($"  【{node.Attack.Name}】 命中！");
        Console.WriteLine($"  伤害: {scaledDamage} (缩放: {_damageScaling:P0})");
        Console.WriteLine($"  帧数: {node.Attack.StartupFrames}/{node.Attack.ActiveFrames}/{node.Attack.RecoveryFrames}");
        Console.WriteLine($"  连段: {_comboCount} Hits | 总伤害: {_comboDamage}");
        Console.WriteLine($"  气量: {_meter:F0}/{MaxMeter}");
        Console.WriteLine($"  ═══════════════════════════════════════════\n");
        
        OnAttackExecuted?.Invoke(node.Attack, scaledDamage);
    }
    
    /// <summary>
    /// 帧更新
    /// </summary>
    public void Update()
    {
        _inputBuffer.Update();
        
        if (_state == CharacterState.Attacking || 
            _state == CharacterState.SpecialMove || 
            _state == CharacterState.SuperMove)
        {
            _currentAttackFrame++;
            
            // 检查攻击是否结束
            if (_currentNode != null && _currentAttackFrame >= _currentNode.Attack.TotalFrames)
            {
                EndAttack();
            }
        }
    }
    
    private void StartCombo()
    {
        _comboCount = 0;
        _comboDamage = 0;
        _damageScaling = 1.0f;
    }
    
    private void EndAttack()
    {
        if (_currentNode?.IsEnder == true || _comboCount > 0)
        {
            EndCombo();
        }
        
        _state = CharacterState.Idle;
        _currentNode = null;
        _currentAttackFrame = 0;
    }
    
    private void EndCombo()
    {
        if (_comboCount > 1)
        {
            Console.WriteLine($"\n  ★★★ {_comboCount} HIT COMBO! 总伤害: {_comboDamage} ★★★\n");
            OnComboEnded?.Invoke(_comboCount, _comboDamage);
        }
        
        _comboCount = 0;
        _comboDamage = 0;
        _damageScaling = 1.0f;
    }
    
    /// <summary>
    /// 打印状态
    /// </summary>
    public void PrintStatus()
    {
        Console.WriteLine($"\n┌────────────────────────────────────────┐");
        Console.WriteLine($"│ 状态: {_state,-15} 帧: {_inputBuffer.CurrentFrame,-8} │");
        Console.WriteLine($"│ 气量: [{new string('█', (int)(_meter / 10)),-10}] {_meter:F0}%       │");
        Console.WriteLine($"│ 当前招式: {CurrentMoveName ?? "无",-20}   │");
        if (_comboCount > 0)
        {
            Console.WriteLine($"│ 连段: {_comboCount} Hits | 伤害: {_comboDamage,-10}     │");
        }
        Console.WriteLine($"└────────────────────────────────────────┘");
    }
    
    private static string FormatButton(InputButton button)
    {
        var parts = new List<string>();
        if (button.HasFlag(InputButton.Up)) parts.Add("↑");
        if (button.HasFlag(InputButton.Down)) parts.Add("↓");
        if (button.HasFlag(InputButton.Back)) parts.Add("←");
        if (button.HasFlag(InputButton.Forward)) parts.Add("→");
        if (button.HasFlag(InputButton.LightPunch)) parts.Add("LP");
        if (button.HasFlag(InputButton.HeavyPunch)) parts.Add("HP");
        if (button.HasFlag(InputButton.LightKick)) parts.Add("LK");
        if (button.HasFlag(InputButton.HeavyKick)) parts.Add("HK");
        return string.Join("+", parts);
    }
}
