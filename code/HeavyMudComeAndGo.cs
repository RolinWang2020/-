using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCDS.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PCDS.Utils.Mud
{
    public class HeavyMudComeAndGoParameters
    {
        public FlowAndPipe flowAndPipe = new FlowAndPipe();

        /// <summary>
        /// 设计的压水眼重浆的体积
        /// </summary>
        public double VolByDesign;

        private Well well = new Well();

        internal Well Well { get => well; set => well = value; }

        public PressureLossOfAnnulus pa;
        public PressureLossOfPipe pp;
    }
    public class ContainerLikeWell
    {
        /// <summary>
        /// 层次结构
        /// </summary>
        public class Hierarchy
        {
            double area = 0;
            int hashCode = 0;
            private Scope scope;

            public double Area { get => area; set => area = value; }

            public int HashCode { get => hashCode; set => hashCode = value; }

            public double Vol { get; set; }

            public Scope Scope { get => scope; set => scope = value; }
        }

        /// <summary>
        /// 微分体积(m3)
        /// </summary>
        double microVol = 0.001;
        /// <summary>
        /// 微分体积(m3)
        /// </summary>
        public double MicroVol { get => microVol; set => microVol = value; }

        /// <summary>
        /// 最深位置(m)
        /// </summary>
        double deepest;
        /// <summary>
        /// 最深位置(m)
        /// </summary>
        public double Deepest { get => deepest; set => deepest = value; }

        /// <summary>
        /// 最深处的索引
        /// </summary>
        public int deepIndex;

        /// <summary>
        /// 微分结构
        /// </summary>
        List<Hierarchy> microStructure = new List<Hierarchy>();
        /// <summary>
        /// 微分结构
        /// </summary>
        public List<Hierarchy> MicroStructure { get => microStructure; set => microStructure = value; }

        /// <summary>
        /// 总体积
        /// </summary>
        public double Vol { get; set; }

        /// <summary>
        /// 设置微分结构
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="annular"></param>
        public void SetMicroStructure(List<Pipe> pipe, List<DrillStr> annular)
        {
            MicroStructure.Clear();
            double sumH = 0;
            Vol = 0;
            pipe.ForEach(item =>
            {
                double area = item.innerDiameter * item.innerDiameter * Math.PI * 0.25;
                int num = (int)(area * item.segment / microVol);
                Vol += area * item.segment;
                double segeH = microVol / area;
                for (int i = 0; i < num; i++)
                {
                    MicroStructure.Add(new Hierarchy()
                    {
                        Scope = new Scope()
                        {
                            LeftSide = sumH,
                            RightSide = sumH + segeH
                        },
                        Area = area,
                        Vol = microVol,
                    });
                    sumH += segeH;
                }
            });

            Deepest = sumH;
            deepIndex = MicroStructure.Count;

            for (int i = annular.Count - 1; i >= 0; i--)
            {
                var item = annular[i];
                double area = ((item.outerDiameter * item.outerDiameter) - (item.innerDiameter * item.innerDiameter)) * Math.PI * 0.25;
                int num = (int)(area * item.segment / microVol);
                double segeH = microVol / area;
                Vol += area * item.segment;
                for (int ii = 0; ii < num; ii++)
                {
                    Hierarchy h = new Hierarchy()
                    {
                        Scope = new Scope()
                        {
                            LeftSide = sumH,
                            RightSide = sumH - segeH
                        },
                        Area = area,
                        Vol = microVol,
                    };
                    MicroStructure.Add(h);
                    sumH -= segeH;
                }
            }

            for (int i = MicroStructure.Count - 1; i >= 0; i--)
            {
                var item = MicroStructure[i];
                if (item.Scope.LeftSide >= 0 && item.Scope.RightSide >= 0)
                    return;
                if (item.Scope.LeftSide >= 0 && item.Scope.RightSide <= 0)
                {
                    item.Scope.RightSide = 0;
                    return;
                }
                MicroStructure.RemoveAt(i);
            }
        }
    }

    public class MicroFluidStructure
    {
        public MudType type;
    }

    public class Fluid
    {
        /// <summary>
        /// 流体类型
        /// </summary>
        public MudType type;

        /// <summary>
        /// 流体相关参数
        /// </summary>
        FluidParameters f;

        public FluidParameters F { get => f; set => f = value; }

        /// <summary>
        /// 流体体积
        /// </summary>
        double vol;

        public double Vol { get => vol; set => vol = value; }

    }

    public class Well
    {
        /// <summary>
        /// 检测器
        /// </summary>
        public class Detection
        {
            public MudType Type;
            public int index;
            public Action doSomething;

            public void Check(List<MudType> fluidMicroStructure)
            {
                if (fluidMicroStructure[index] != Type)
                {
                    doSomething();
                    Type = fluidMicroStructure[index];
                }
            }
        }

        /// <summary>
        /// 流体队列
        /// </summary>
        public List<Fluid> fluids = new List<Fluid>();

        public ContainerLikeWell container = new ContainerLikeWell();

        public List<MudType> fluidMicroStructure = new List<MudType>();

        public Detection deepPoint = new Detection();

        public Detection outPoint = new Detection();

        /// <summary>
        /// 设置微分结构
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="annular"></param>
        public void SetMicroStructure(List<Pipe> pipe, List<DrillStr> annular)
        {
            container.SetMicroStructure(pipe, annular);

        }

        /// <summary>
        /// 向井里注入液体
        /// </summary>
        public void InjectionMicroStructure()
        {
            

            int all = container.MicroStructure.Count;
            double sumVol = GetSumVol();
            fluids.ForEach(item =>
            {
                int loop = (int)(all * (item.Vol / sumVol));
                for (int i = 0; i < loop; i++)
                {
                    fluidMicroStructure.Add(item.type);
                }
            });

            if (fluidMicroStructure.Count < all)
            {
                Fluid item = fluids.Last();
                for (int i = fluidMicroStructure.Count; i < all; i++)
                {
                    fluidMicroStructure.Add(item.type);
                }
            }
            else
            {
                for (int i = fluidMicroStructure.Count - 1; i >= all; i--)
                {
                    fluidMicroStructure.RemoveAt(i);
                }



            }
            SetDetection();
        }

        /// <summary>
        /// 设置监视器
        /// </summary>
        private void SetDetection()
        {
            void InitPoint(int i, MudType t, Detection d)
            {
                d.index = i;
                d.Type = t;
            }
            InitPoint(container.MicroStructure.Count - 1, fluids.Last().type, outPoint);
            InitPoint(container.deepIndex, fluidMicroStructure[container.deepIndex], deepPoint);
        }

        /// <summary>
        /// 流动体积(m3)
        /// </summary>
        /// <param name="Vol"></param>
        public void Move(double Vol)
        {
            int num = (int)(Vol / container.MicroVol);
            MudType type = fluids.First().type;
            for (int i = 0; i < num; i++)
            {
                fluidMicroStructure.RemoveAt(outPoint.index);
                fluidMicroStructure.Insert(0, type);
            }


            deepPoint.Check(fluidMicroStructure);
            outPoint.Check(fluidMicroStructure);
            VolReduce(Vol);
        }

        /// <summary>
        /// 改变流体体积
        /// </summary>
        /// <param name="Q"></param>
        private void VolReduce(double Q)
        {
            fluids.First().Vol += Q;
            for (int i = fluids.Count - 1; i >= 0; i--)
            {
                var item = fluids[i];
                var step = item.Vol;
                item.Vol = Math.Max(0, step - Q);
                Q = Math.Max(0, Q - step);
            }
        }

        public void ChickAndFill()
        {
            int difference = container.MicroStructure.Count - fluidMicroStructure.Count;
            if (difference > 0)
            {
                MudType type = fluids.First().type;
                for (int i = 0; i < difference; i++)
                {
                    fluidMicroStructure.Insert(0, type);
                }
                fluids.First().Vol += difference * container.MicroVol;
            }
            else
            {
                for (int i = 1; i < Math.Abs(difference); i++)
                {
                    fluidMicroStructure.RemoveAt(0);
                }

                double Q = Math.Abs(difference) * container.MicroVol;

                if (Q > fluids.Last().Vol)
                    ErrorMsg("替出前钻头深度过低，压水眼重浆全部流出");

                for (int i = fluids.Count - 1; i >= 0; i--)
                {
                    var item = fluids[i];
                    var step = item.Vol;
                    item.Vol = Math.Max(0, step - Q);
                    Q = Math.Max(0, Q - step);
                }
            }
            SetDetection();
        }

        public (FluidGradient p, FluidGradient a) GetFluidGradient()
        {
            FluidGradient p = new FluidGradient();
            FluidGradient a = new FluidGradient();

            int Index = fluids
                .Select((item, index) => (item, index))
                .Where(item => item.item.type == deepPoint.Type).ToList()[0]
                .index;

            double sumVol = GetSumVol();
            int all = container.MicroStructure.Count;
            double halfVol = 0;

            List<int> dividingLine = fluids.Select(item =>
            {
                halfVol += item.Vol;
                int loc = (int)(all * (halfVol / sumVol)) - 1;
                return Math.Max(loc, 0);
            }).ToList();

           
            double start = 0;
            var pf = fluids.Where((item, index) => index <= Index).ToList();

            for (int i = 0; i < pf.Count; i++)
            {
                var item = pf[i];
                double r = 0; 
                if (i==pf.Count-1)
                    r = container.Deepest;
                else
                    r = container.MicroStructure[dividingLine[i]].Scope.RightSide;

                p.Gradient.Add(new Scope()
                {
                    LeftSide = start,
                    RightSide = r
                });
                p.FluidParameterss.Add(item.F);
                p.types.Add(item.type);
                start = r;
            }

            dividingLine.Reverse();
            dividingLine.RemoveAt(0);
            start = 0;
            var af = fluids.Where((item, index) => index >= Index).ToList();
            af.Reverse();
            for (int i = 0; i < af.Count; i++)
            {
                var item = af[i];
                double r = 0;
                if (i == af.Count - 1)
                    r = container.Deepest;
                else
                    r = container.MicroStructure[dividingLine[i]].Scope.RightSide;

                a.Gradient.Add(new Scope()
                {
                    LeftSide = start,
                    RightSide = r,
                });
                a.FluidParameterss.Add(item.F);
                a.types.Add(item.type);

                start = r;
            }

            return (p, a);
        }

        private double GetSumVol()
        {
            double sumVol = 0;
            fluids.ForEach(item => sumVol += item.Vol);
            return sumVol;
        }

        void ErrorMsg(string msg)
        {
            var logService = new LogService();

            logService.SendNotice("重浆驱替过程", msg, 2);
        }
    }

    public abstract class HeavyMudComeAndGo : HeavyMudComeAndGoParameters
    {
        public HeavyMudComeAndGo()
        {
            //LoadStruct();
            //LoadFluid();
            //Well.Done = Step1;
        }
        public abstract void LoadFluid();

        public abstract void LoadStruct();

        /// <summary>
        /// 压水眼重浆到达井底
        /// </summary>
        public virtual void PILLCome()
        {
            Well.deepPoint.doSomething = SLUGCome;  
        }

        /// <summary>
        /// 重浆到达井底
        /// </summary>
        public virtual void SLUGCome()
        {
            Well.deepPoint.doSomething = MudCome;
        }

        /// <summary>
        /// 压水眼重浆出井
        /// </summary>
        public virtual void PILLGo()
        {
            Well.fluids.Remove(Well.fluids.Last());
            Well.outPoint.doSomething = SLUGGo;
        }

        /// <summary>
        /// 清浆出井
        /// </summary>
        public virtual void MudGo()
        {
            Well.fluids.Remove(Well.fluids.Last());
            Well.ChickAndFill();
            Well.outPoint.doSomething = PILLGo;
        }

        /// <summary>
        /// 清浆到达井底
        /// </summary>
        public virtual void MudCome()
        {

        }

        /// <summary>
        /// 重浆出井
        /// </summary>
        public virtual void SLUGGo()
        {
            Well.fluids.Remove(Well.fluids.Last());
            
            Well.outPoint.doSomething = MudGoAgain;
        }

        /// <summary>
        /// 清浆再一次出井
        /// </summary>
        public virtual void MudGoAgain()
        {
            Well.fluids.Remove(Well.fluids.Last());
            Well.fluidMicroStructure.Clear();
            Well.container.MicroStructure.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Qin"></param>
        /// <returns></returns>
        public JObject Run(double Qin)
        {
            var ans = new JObject();
            Move(Qin);

            //计算内外压耗
            (FluidGradient p, FluidGradient a) = Well.GetFluidGradient();

            flowAndPipe.fluidps = a.FluidParameterss.First();
            (pp, pa) = 布希迪西的工厂.GetPressureOperator(flowAndPipe.fluidps.mode);
            pa.flowAndPipe = flowAndPipe;

            flowAndPipe.fluidps = p.FluidParameterss.First();
            pp.flowAndPipe = flowAndPipe;


            ans["Annualr"] = pa.GetPressLossInAnnulus(Qin, a);
            ans["Pipe"] = pp.PressLossInPipe(Qin, p);


            ans["in"] = JsonConvert.SerializeObject(p);
            ans["out"] = JsonConvert.SerializeObject(a);

            //计算内静液柱压力
            double hp = 0;
            for (int i = 0; i < p.FluidParameterss.Count; i++)
            {
                hp += p.FluidParameterss[i].rou * 9.8 * p.Gradient[i].Range / 1000000;
            }
            ans["Hp"] = hp;

            ////计算外静液柱压力
            double ha = 0;
            for (int i = 0; i < a.FluidParameterss.Count; i++)
            {
                ha += a.FluidParameterss[i].rou * 9.8 * a.Gradient[i].Range / 1000000;
            }
            ans["Ha"] = ha;

            ans["PILLVol"] = GetVol(MudType.压水眼重浆);//
            ans["SLUVol"] = GetVol(MudType.重浆);
            //计算井底压力
            ans["WellDownP"] = Math.Max(ha, hp);
            ans["WellEnter"] = Math.Abs(ha - hp);

            return ans;
            double GetVol(MudType m)
            {
                var select = Well.fluids.Where(item => item.type == m).ToList();
                if (select.Count > 0)
                    return select[0].Vol;
                else
                    return 0;
            }
        }

        public void Move(double Q)
        {
            Well.Move(Q);
        }
    }

    public class HeavyMudComeAndGoTest : HeavyMudComeAndGo
    {

        public HeavyMudComeAndGoTest()
        {
            flowAndPipe.BitSize = 4000;
            flowAndPipe.ROP = 100;
        }
        public override void LoadFluid()
        {

            var item1 = new FluidParameters();

            item1.mode = FluidMode.幂律;

            item1.rou = 1150;

            item1.k = 1;

            item1.n = 2;

            Well.fluids.Add(new Fluid() { F = item1, Vol = 0, type = MudType.重浆 });


            var item2 = new FluidParameters();

            item2.mode = FluidMode.幂律;

            item2.rou = 2150;

            item2.k = 10;

            item2.n = 3;

            Well.fluids.Add(new Fluid() { F = item2, Vol = 10, type = MudType.压水眼重浆 });

            var item3 = new FluidParameters();

            item3.mode = FluidMode.幂律;

            item3.rou = 3150;

            item3.k = 5;

            item3.n = 4;

            Well.fluids.Add(new Fluid() { F = item3, Vol = Well.container.Vol - 10, type = MudType.清浆 });





            Well.InjectionMicroStructure();

            Well.deepPoint.doSomething = PILLCome;
            Well.outPoint.doSomething = PILLGo;

            Well.Move(10);


            Well.Move(10);


            Well.Move(10);


            Well.Move(10);


            Well.Move(10);


            Well.Move(10);


            Well.Move(10);

        }

        

        public override void LoadStruct()
        {
            WellBoreService wellBoreService = new WellBoreService();
            DrillStringService drillStringService = new DrillStringService();

            (flowAndPipe.annular, flowAndPipe.pipe) = __UtilityWorkFlow.InitPipeAndAnnular(4000, wellBoreService.Get(), drillStringService.Get());
            Well.SetMicroStructure(flowAndPipe.pipe, flowAndPipe.annular);

        }


        public override void PILLCome()
        {
            LogUtil.Info("12");
            Well.deepPoint.doSomething = SLUGCome;
            Well.outPoint.doSomething = SLUGCome;
        }


        public override void SLUGCome()
        {

        }

        public override void PILLGo()
        {

        }

        public override void MudGo()
        {

        }

        public override void MudCome()
        {

        }

        public override void SLUGGo()
        {

        }
    }

    class HeavyMudComeAndGoForServer : HeavyMudComeAndGo
    {
        /// <summary>
        /// 是否继续生成
        /// </summary>
        public bool isContinue = true;

        /// <summary>
        /// 设计相关的信息
        /// </summary>
        public JObject DesignMsg;

        JArray timeLine;

        public JObject GenerateTimeline(double Qin)
        {

            timeLine = new JArray();
            DesignMsg = new JObject();

            while (isContinue)
            {
                timeLine.Add(Run(Qin));
            }

            int Line = DesignMsg["MudGo"].ToObject<int>();


            var timeLines = timeLine.GroupBy(item => timeLine.IndexOf(item) < Line).Select(g => new JArray(g)).ToArray();

            try
            {
                DesignMsg["BeforeTimeline"] = timeLines[0];

                DesignMsg["AfterTimeline"] = timeLines[1];
            }
            catch(Exception e)
            {
                LogUtil.Error("", e);
            }

            DesignMsg["WellBottom"] = wellboreList.Last().depth;



            return DesignMsg;
        }
        public  List<drillstring> beforeDrillStringList;
        public  List<drillstring> afterDrillStringList;
        public  List<wellbore> wellboreList;
        public  List<FluidParameters> fluidsList;
        public  PressureWaterHoleParameters pressureWaterHoleParameters;
        public  double BeforeBitsize = 0;
        public double AfterBitsize = 0;

        public override void LoadFluid()
        {

            Well.fluids.Add(new Fluid() { F = fluidsList[2], Vol = 0, type = MudType.重浆 });
            Well.fluids.Add(new Fluid() { F = fluidsList[1], Vol = pressureWaterHoleParameters.PILLVol, type = MudType.压水眼重浆 });
            Well.fluids.Add(new Fluid() { F = fluidsList[0], Vol = Well.container.Vol- pressureWaterHoleParameters.PILLVol, type = MudType.清浆 });

            Well.InjectionMicroStructure();

            Well.deepPoint.doSomething = PILLCome;
            Well.outPoint.doSomething = MudGo;

        }



        public override void LoadStruct()
        {
            (flowAndPipe.annular, flowAndPipe.pipe) = __UtilityWorkFlow.InitPipeAndAnnular(BeforeBitsize, wellboreList, beforeDrillStringList);

            Well.SetMicroStructure(flowAndPipe.pipe, flowAndPipe.annular);

        }


        public override void PILLCome()
        {
            DesignMsg["PILLCome"] = timeLine.Count;
            base.PILLCome();
        }


        public override void SLUGCome()
        {
            DesignMsg["SLUGCome"] = timeLine.Count;
            base.SLUGCome();
        }

        public override void PILLGo()
        {
            ProgressMsg("模拟进度：50%");
            DesignMsg["PILLGo"] = timeLine.Count;
            base.PILLGo();
        }

        public override void MudGo()
        {
            ProgressMsg("模拟进度：40%");
            DesignMsg["MudGo"] = timeLine.Count;
            (flowAndPipe.annular, flowAndPipe.pipe) = __UtilityWorkFlow.InitPipeAndAnnular(AfterBitsize, wellboreList, afterDrillStringList);

            Well.SetMicroStructure(flowAndPipe.pipe, flowAndPipe.annular);
            Well.fluids.Insert(0,new Fluid() { F = fluidsList[0], Vol = 0, type = MudType.清浆 });
            base.MudGo();
        }

        public override void MudCome()
        {
            DesignMsg["MudCome"] = timeLine.Count;
            base.MudCome();
        }

        public override void SLUGGo()
        {
            ProgressMsg("模拟进度：60%");
            DesignMsg["SLUGGo"] = timeLine.Count;
            isContinue = false;
            base.SLUGGo();
        }

        void ProgressMsg(string msg)
        {
            var logService = new LogService();

            logService.SendNotice("重浆模拟进度", msg, 1);
        }
    }
}
