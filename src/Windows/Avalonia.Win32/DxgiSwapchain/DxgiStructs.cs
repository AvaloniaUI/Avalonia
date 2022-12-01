using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.DxgiSwapchain
{
#nullable enable
    public unsafe struct HANDLE
    {
        public readonly void* Value;

        public HANDLE(void* value)
        {
            Value = value;
        }

        public static HANDLE INVALID_VALUE => new HANDLE((void*)(-1));

        public static HANDLE NULL => new HANDLE(null);

        public static bool operator ==(HANDLE left, HANDLE right) => left.Value == right.Value;

        public static bool operator !=(HANDLE left, HANDLE right) => left.Value != right.Value;

        public override bool Equals(object? obj) => (obj is HANDLE other) && Equals(other);

        public bool Equals(HANDLE other) => ((nuint)(Value)).Equals((nuint)(other.Value));

        public override int GetHashCode() => ((nuint)(Value)).GetHashCode();

        public override string ToString() => ((IntPtr)Value).ToString();
    }

    internal unsafe partial struct MONITORINFOEXW
    {
        internal MONITORINFO Base;

        internal fixed ushort szDevice[32];
    }

    internal unsafe struct DXGI_GAMMA_CONTROL
    {
        public DXGI_RGB Scale;

        public DXGI_RGB Offset;

        public _GammaCurve_e__FixedBuffer GammaCurve;

        public partial struct _GammaCurve_e__FixedBuffer
        {
            public DXGI_RGB e0;
            public DXGI_RGB e1;
            public DXGI_RGB e2;
            public DXGI_RGB e3;
            public DXGI_RGB e4;
            public DXGI_RGB e5;
            public DXGI_RGB e6;
            public DXGI_RGB e7;
            public DXGI_RGB e8;
            public DXGI_RGB e9;
            public DXGI_RGB e10;
            public DXGI_RGB e11;
            public DXGI_RGB e12;
            public DXGI_RGB e13;
            public DXGI_RGB e14;
            public DXGI_RGB e15;
            public DXGI_RGB e16;
            public DXGI_RGB e17;
            public DXGI_RGB e18;
            public DXGI_RGB e19;
            public DXGI_RGB e20;
            public DXGI_RGB e21;
            public DXGI_RGB e22;
            public DXGI_RGB e23;
            public DXGI_RGB e24;
            public DXGI_RGB e25;
            public DXGI_RGB e26;
            public DXGI_RGB e27;
            public DXGI_RGB e28;
            public DXGI_RGB e29;
            public DXGI_RGB e30;
            public DXGI_RGB e31;
            public DXGI_RGB e32;
            public DXGI_RGB e33;
            public DXGI_RGB e34;
            public DXGI_RGB e35;
            public DXGI_RGB e36;
            public DXGI_RGB e37;
            public DXGI_RGB e38;
            public DXGI_RGB e39;
            public DXGI_RGB e40;
            public DXGI_RGB e41;
            public DXGI_RGB e42;
            public DXGI_RGB e43;
            public DXGI_RGB e44;
            public DXGI_RGB e45;
            public DXGI_RGB e46;
            public DXGI_RGB e47;
            public DXGI_RGB e48;
            public DXGI_RGB e49;
            public DXGI_RGB e50;
            public DXGI_RGB e51;
            public DXGI_RGB e52;
            public DXGI_RGB e53;
            public DXGI_RGB e54;
            public DXGI_RGB e55;
            public DXGI_RGB e56;
            public DXGI_RGB e57;
            public DXGI_RGB e58;
            public DXGI_RGB e59;
            public DXGI_RGB e60;
            public DXGI_RGB e61;
            public DXGI_RGB e62;
            public DXGI_RGB e63;
            public DXGI_RGB e64;
            public DXGI_RGB e65;
            public DXGI_RGB e66;
            public DXGI_RGB e67;
            public DXGI_RGB e68;
            public DXGI_RGB e69;
            public DXGI_RGB e70;
            public DXGI_RGB e71;
            public DXGI_RGB e72;
            public DXGI_RGB e73;
            public DXGI_RGB e74;
            public DXGI_RGB e75;
            public DXGI_RGB e76;
            public DXGI_RGB e77;
            public DXGI_RGB e78;
            public DXGI_RGB e79;
            public DXGI_RGB e80;
            public DXGI_RGB e81;
            public DXGI_RGB e82;
            public DXGI_RGB e83;
            public DXGI_RGB e84;
            public DXGI_RGB e85;
            public DXGI_RGB e86;
            public DXGI_RGB e87;
            public DXGI_RGB e88;
            public DXGI_RGB e89;
            public DXGI_RGB e90;
            public DXGI_RGB e91;
            public DXGI_RGB e92;
            public DXGI_RGB e93;
            public DXGI_RGB e94;
            public DXGI_RGB e95;
            public DXGI_RGB e96;
            public DXGI_RGB e97;
            public DXGI_RGB e98;
            public DXGI_RGB e99;
            public DXGI_RGB e100;
            public DXGI_RGB e101;
            public DXGI_RGB e102;
            public DXGI_RGB e103;
            public DXGI_RGB e104;
            public DXGI_RGB e105;
            public DXGI_RGB e106;
            public DXGI_RGB e107;
            public DXGI_RGB e108;
            public DXGI_RGB e109;
            public DXGI_RGB e110;
            public DXGI_RGB e111;
            public DXGI_RGB e112;
            public DXGI_RGB e113;
            public DXGI_RGB e114;
            public DXGI_RGB e115;
            public DXGI_RGB e116;
            public DXGI_RGB e117;
            public DXGI_RGB e118;
            public DXGI_RGB e119;
            public DXGI_RGB e120;
            public DXGI_RGB e121;
            public DXGI_RGB e122;
            public DXGI_RGB e123;
            public DXGI_RGB e124;
            public DXGI_RGB e125;
            public DXGI_RGB e126;
            public DXGI_RGB e127;
            public DXGI_RGB e128;
            public DXGI_RGB e129;
            public DXGI_RGB e130;
            public DXGI_RGB e131;
            public DXGI_RGB e132;
            public DXGI_RGB e133;
            public DXGI_RGB e134;
            public DXGI_RGB e135;
            public DXGI_RGB e136;
            public DXGI_RGB e137;
            public DXGI_RGB e138;
            public DXGI_RGB e139;
            public DXGI_RGB e140;
            public DXGI_RGB e141;
            public DXGI_RGB e142;
            public DXGI_RGB e143;
            public DXGI_RGB e144;
            public DXGI_RGB e145;
            public DXGI_RGB e146;
            public DXGI_RGB e147;
            public DXGI_RGB e148;
            public DXGI_RGB e149;
            public DXGI_RGB e150;
            public DXGI_RGB e151;
            public DXGI_RGB e152;
            public DXGI_RGB e153;
            public DXGI_RGB e154;
            public DXGI_RGB e155;
            public DXGI_RGB e156;
            public DXGI_RGB e157;
            public DXGI_RGB e158;
            public DXGI_RGB e159;
            public DXGI_RGB e160;
            public DXGI_RGB e161;
            public DXGI_RGB e162;
            public DXGI_RGB e163;
            public DXGI_RGB e164;
            public DXGI_RGB e165;
            public DXGI_RGB e166;
            public DXGI_RGB e167;
            public DXGI_RGB e168;
            public DXGI_RGB e169;
            public DXGI_RGB e170;
            public DXGI_RGB e171;
            public DXGI_RGB e172;
            public DXGI_RGB e173;
            public DXGI_RGB e174;
            public DXGI_RGB e175;
            public DXGI_RGB e176;
            public DXGI_RGB e177;
            public DXGI_RGB e178;
            public DXGI_RGB e179;
            public DXGI_RGB e180;
            public DXGI_RGB e181;
            public DXGI_RGB e182;
            public DXGI_RGB e183;
            public DXGI_RGB e184;
            public DXGI_RGB e185;
            public DXGI_RGB e186;
            public DXGI_RGB e187;
            public DXGI_RGB e188;
            public DXGI_RGB e189;
            public DXGI_RGB e190;
            public DXGI_RGB e191;
            public DXGI_RGB e192;
            public DXGI_RGB e193;
            public DXGI_RGB e194;
            public DXGI_RGB e195;
            public DXGI_RGB e196;
            public DXGI_RGB e197;
            public DXGI_RGB e198;
            public DXGI_RGB e199;
            public DXGI_RGB e200;
            public DXGI_RGB e201;
            public DXGI_RGB e202;
            public DXGI_RGB e203;
            public DXGI_RGB e204;
            public DXGI_RGB e205;
            public DXGI_RGB e206;
            public DXGI_RGB e207;
            public DXGI_RGB e208;
            public DXGI_RGB e209;
            public DXGI_RGB e210;
            public DXGI_RGB e211;
            public DXGI_RGB e212;
            public DXGI_RGB e213;
            public DXGI_RGB e214;
            public DXGI_RGB e215;
            public DXGI_RGB e216;
            public DXGI_RGB e217;
            public DXGI_RGB e218;
            public DXGI_RGB e219;
            public DXGI_RGB e220;
            public DXGI_RGB e221;
            public DXGI_RGB e222;
            public DXGI_RGB e223;
            public DXGI_RGB e224;
            public DXGI_RGB e225;
            public DXGI_RGB e226;
            public DXGI_RGB e227;
            public DXGI_RGB e228;
            public DXGI_RGB e229;
            public DXGI_RGB e230;
            public DXGI_RGB e231;
            public DXGI_RGB e232;
            public DXGI_RGB e233;
            public DXGI_RGB e234;
            public DXGI_RGB e235;
            public DXGI_RGB e236;
            public DXGI_RGB e237;
            public DXGI_RGB e238;
            public DXGI_RGB e239;
            public DXGI_RGB e240;
            public DXGI_RGB e241;
            public DXGI_RGB e242;
            public DXGI_RGB e243;
            public DXGI_RGB e244;
            public DXGI_RGB e245;
            public DXGI_RGB e246;
            public DXGI_RGB e247;
            public DXGI_RGB e248;
            public DXGI_RGB e249;
            public DXGI_RGB e250;
            public DXGI_RGB e251;
            public DXGI_RGB e252;
            public DXGI_RGB e253;
            public DXGI_RGB e254;
            public DXGI_RGB e255;
            public DXGI_RGB e256;
            public DXGI_RGB e257;
            public DXGI_RGB e258;
            public DXGI_RGB e259;
            public DXGI_RGB e260;
            public DXGI_RGB e261;
            public DXGI_RGB e262;
            public DXGI_RGB e263;
            public DXGI_RGB e264;
            public DXGI_RGB e265;
            public DXGI_RGB e266;
            public DXGI_RGB e267;
            public DXGI_RGB e268;
            public DXGI_RGB e269;
            public DXGI_RGB e270;
            public DXGI_RGB e271;
            public DXGI_RGB e272;
            public DXGI_RGB e273;
            public DXGI_RGB e274;
            public DXGI_RGB e275;
            public DXGI_RGB e276;
            public DXGI_RGB e277;
            public DXGI_RGB e278;
            public DXGI_RGB e279;
            public DXGI_RGB e280;
            public DXGI_RGB e281;
            public DXGI_RGB e282;
            public DXGI_RGB e283;
            public DXGI_RGB e284;
            public DXGI_RGB e285;
            public DXGI_RGB e286;
            public DXGI_RGB e287;
            public DXGI_RGB e288;
            public DXGI_RGB e289;
            public DXGI_RGB e290;
            public DXGI_RGB e291;
            public DXGI_RGB e292;
            public DXGI_RGB e293;
            public DXGI_RGB e294;
            public DXGI_RGB e295;
            public DXGI_RGB e296;
            public DXGI_RGB e297;
            public DXGI_RGB e298;
            public DXGI_RGB e299;
            public DXGI_RGB e300;
            public DXGI_RGB e301;
            public DXGI_RGB e302;
            public DXGI_RGB e303;
            public DXGI_RGB e304;
            public DXGI_RGB e305;
            public DXGI_RGB e306;
            public DXGI_RGB e307;
            public DXGI_RGB e308;
            public DXGI_RGB e309;
            public DXGI_RGB e310;
            public DXGI_RGB e311;
            public DXGI_RGB e312;
            public DXGI_RGB e313;
            public DXGI_RGB e314;
            public DXGI_RGB e315;
            public DXGI_RGB e316;
            public DXGI_RGB e317;
            public DXGI_RGB e318;
            public DXGI_RGB e319;
            public DXGI_RGB e320;
            public DXGI_RGB e321;
            public DXGI_RGB e322;
            public DXGI_RGB e323;
            public DXGI_RGB e324;
            public DXGI_RGB e325;
            public DXGI_RGB e326;
            public DXGI_RGB e327;
            public DXGI_RGB e328;
            public DXGI_RGB e329;
            public DXGI_RGB e330;
            public DXGI_RGB e331;
            public DXGI_RGB e332;
            public DXGI_RGB e333;
            public DXGI_RGB e334;
            public DXGI_RGB e335;
            public DXGI_RGB e336;
            public DXGI_RGB e337;
            public DXGI_RGB e338;
            public DXGI_RGB e339;
            public DXGI_RGB e340;
            public DXGI_RGB e341;
            public DXGI_RGB e342;
            public DXGI_RGB e343;
            public DXGI_RGB e344;
            public DXGI_RGB e345;
            public DXGI_RGB e346;
            public DXGI_RGB e347;
            public DXGI_RGB e348;
            public DXGI_RGB e349;
            public DXGI_RGB e350;
            public DXGI_RGB e351;
            public DXGI_RGB e352;
            public DXGI_RGB e353;
            public DXGI_RGB e354;
            public DXGI_RGB e355;
            public DXGI_RGB e356;
            public DXGI_RGB e357;
            public DXGI_RGB e358;
            public DXGI_RGB e359;
            public DXGI_RGB e360;
            public DXGI_RGB e361;
            public DXGI_RGB e362;
            public DXGI_RGB e363;
            public DXGI_RGB e364;
            public DXGI_RGB e365;
            public DXGI_RGB e366;
            public DXGI_RGB e367;
            public DXGI_RGB e368;
            public DXGI_RGB e369;
            public DXGI_RGB e370;
            public DXGI_RGB e371;
            public DXGI_RGB e372;
            public DXGI_RGB e373;
            public DXGI_RGB e374;
            public DXGI_RGB e375;
            public DXGI_RGB e376;
            public DXGI_RGB e377;
            public DXGI_RGB e378;
            public DXGI_RGB e379;
            public DXGI_RGB e380;
            public DXGI_RGB e381;
            public DXGI_RGB e382;
            public DXGI_RGB e383;
            public DXGI_RGB e384;
            public DXGI_RGB e385;
            public DXGI_RGB e386;
            public DXGI_RGB e387;
            public DXGI_RGB e388;
            public DXGI_RGB e389;
            public DXGI_RGB e390;
            public DXGI_RGB e391;
            public DXGI_RGB e392;
            public DXGI_RGB e393;
            public DXGI_RGB e394;
            public DXGI_RGB e395;
            public DXGI_RGB e396;
            public DXGI_RGB e397;
            public DXGI_RGB e398;
            public DXGI_RGB e399;
            public DXGI_RGB e400;
            public DXGI_RGB e401;
            public DXGI_RGB e402;
            public DXGI_RGB e403;
            public DXGI_RGB e404;
            public DXGI_RGB e405;
            public DXGI_RGB e406;
            public DXGI_RGB e407;
            public DXGI_RGB e408;
            public DXGI_RGB e409;
            public DXGI_RGB e410;
            public DXGI_RGB e411;
            public DXGI_RGB e412;
            public DXGI_RGB e413;
            public DXGI_RGB e414;
            public DXGI_RGB e415;
            public DXGI_RGB e416;
            public DXGI_RGB e417;
            public DXGI_RGB e418;
            public DXGI_RGB e419;
            public DXGI_RGB e420;
            public DXGI_RGB e421;
            public DXGI_RGB e422;
            public DXGI_RGB e423;
            public DXGI_RGB e424;
            public DXGI_RGB e425;
            public DXGI_RGB e426;
            public DXGI_RGB e427;
            public DXGI_RGB e428;
            public DXGI_RGB e429;
            public DXGI_RGB e430;
            public DXGI_RGB e431;
            public DXGI_RGB e432;
            public DXGI_RGB e433;
            public DXGI_RGB e434;
            public DXGI_RGB e435;
            public DXGI_RGB e436;
            public DXGI_RGB e437;
            public DXGI_RGB e438;
            public DXGI_RGB e439;
            public DXGI_RGB e440;
            public DXGI_RGB e441;
            public DXGI_RGB e442;
            public DXGI_RGB e443;
            public DXGI_RGB e444;
            public DXGI_RGB e445;
            public DXGI_RGB e446;
            public DXGI_RGB e447;
            public DXGI_RGB e448;
            public DXGI_RGB e449;
            public DXGI_RGB e450;
            public DXGI_RGB e451;
            public DXGI_RGB e452;
            public DXGI_RGB e453;
            public DXGI_RGB e454;
            public DXGI_RGB e455;
            public DXGI_RGB e456;
            public DXGI_RGB e457;
            public DXGI_RGB e458;
            public DXGI_RGB e459;
            public DXGI_RGB e460;
            public DXGI_RGB e461;
            public DXGI_RGB e462;
            public DXGI_RGB e463;
            public DXGI_RGB e464;
            public DXGI_RGB e465;
            public DXGI_RGB e466;
            public DXGI_RGB e467;
            public DXGI_RGB e468;
            public DXGI_RGB e469;
            public DXGI_RGB e470;
            public DXGI_RGB e471;
            public DXGI_RGB e472;
            public DXGI_RGB e473;
            public DXGI_RGB e474;
            public DXGI_RGB e475;
            public DXGI_RGB e476;
            public DXGI_RGB e477;
            public DXGI_RGB e478;
            public DXGI_RGB e479;
            public DXGI_RGB e480;
            public DXGI_RGB e481;
            public DXGI_RGB e482;
            public DXGI_RGB e483;
            public DXGI_RGB e484;
            public DXGI_RGB e485;
            public DXGI_RGB e486;
            public DXGI_RGB e487;
            public DXGI_RGB e488;
            public DXGI_RGB e489;
            public DXGI_RGB e490;
            public DXGI_RGB e491;
            public DXGI_RGB e492;
            public DXGI_RGB e493;
            public DXGI_RGB e494;
            public DXGI_RGB e495;
            public DXGI_RGB e496;
            public DXGI_RGB e497;
            public DXGI_RGB e498;
            public DXGI_RGB e499;
            public DXGI_RGB e500;
            public DXGI_RGB e501;
            public DXGI_RGB e502;
            public DXGI_RGB e503;
            public DXGI_RGB e504;
            public DXGI_RGB e505;
            public DXGI_RGB e506;
            public DXGI_RGB e507;
            public DXGI_RGB e508;
            public DXGI_RGB e509;
            public DXGI_RGB e510;
            public DXGI_RGB e511;
            public DXGI_RGB e512;
            public DXGI_RGB e513;
            public DXGI_RGB e514;
            public DXGI_RGB e515;
            public DXGI_RGB e516;
            public DXGI_RGB e517;
            public DXGI_RGB e518;
            public DXGI_RGB e519;
            public DXGI_RGB e520;
            public DXGI_RGB e521;
            public DXGI_RGB e522;
            public DXGI_RGB e523;
            public DXGI_RGB e524;
            public DXGI_RGB e525;
            public DXGI_RGB e526;
            public DXGI_RGB e527;
            public DXGI_RGB e528;
            public DXGI_RGB e529;
            public DXGI_RGB e530;
            public DXGI_RGB e531;
            public DXGI_RGB e532;
            public DXGI_RGB e533;
            public DXGI_RGB e534;
            public DXGI_RGB e535;
            public DXGI_RGB e536;
            public DXGI_RGB e537;
            public DXGI_RGB e538;
            public DXGI_RGB e539;
            public DXGI_RGB e540;
            public DXGI_RGB e541;
            public DXGI_RGB e542;
            public DXGI_RGB e543;
            public DXGI_RGB e544;
            public DXGI_RGB e545;
            public DXGI_RGB e546;
            public DXGI_RGB e547;
            public DXGI_RGB e548;
            public DXGI_RGB e549;
            public DXGI_RGB e550;
            public DXGI_RGB e551;
            public DXGI_RGB e552;
            public DXGI_RGB e553;
            public DXGI_RGB e554;
            public DXGI_RGB e555;
            public DXGI_RGB e556;
            public DXGI_RGB e557;
            public DXGI_RGB e558;
            public DXGI_RGB e559;
            public DXGI_RGB e560;
            public DXGI_RGB e561;
            public DXGI_RGB e562;
            public DXGI_RGB e563;
            public DXGI_RGB e564;
            public DXGI_RGB e565;
            public DXGI_RGB e566;
            public DXGI_RGB e567;
            public DXGI_RGB e568;
            public DXGI_RGB e569;
            public DXGI_RGB e570;
            public DXGI_RGB e571;
            public DXGI_RGB e572;
            public DXGI_RGB e573;
            public DXGI_RGB e574;
            public DXGI_RGB e575;
            public DXGI_RGB e576;
            public DXGI_RGB e577;
            public DXGI_RGB e578;
            public DXGI_RGB e579;
            public DXGI_RGB e580;
            public DXGI_RGB e581;
            public DXGI_RGB e582;
            public DXGI_RGB e583;
            public DXGI_RGB e584;
            public DXGI_RGB e585;
            public DXGI_RGB e586;
            public DXGI_RGB e587;
            public DXGI_RGB e588;
            public DXGI_RGB e589;
            public DXGI_RGB e590;
            public DXGI_RGB e591;
            public DXGI_RGB e592;
            public DXGI_RGB e593;
            public DXGI_RGB e594;
            public DXGI_RGB e595;
            public DXGI_RGB e596;
            public DXGI_RGB e597;
            public DXGI_RGB e598;
            public DXGI_RGB e599;
            public DXGI_RGB e600;
            public DXGI_RGB e601;
            public DXGI_RGB e602;
            public DXGI_RGB e603;
            public DXGI_RGB e604;
            public DXGI_RGB e605;
            public DXGI_RGB e606;
            public DXGI_RGB e607;
            public DXGI_RGB e608;
            public DXGI_RGB e609;
            public DXGI_RGB e610;
            public DXGI_RGB e611;
            public DXGI_RGB e612;
            public DXGI_RGB e613;
            public DXGI_RGB e614;
            public DXGI_RGB e615;
            public DXGI_RGB e616;
            public DXGI_RGB e617;
            public DXGI_RGB e618;
            public DXGI_RGB e619;
            public DXGI_RGB e620;
            public DXGI_RGB e621;
            public DXGI_RGB e622;
            public DXGI_RGB e623;
            public DXGI_RGB e624;
            public DXGI_RGB e625;
            public DXGI_RGB e626;
            public DXGI_RGB e627;
            public DXGI_RGB e628;
            public DXGI_RGB e629;
            public DXGI_RGB e630;
            public DXGI_RGB e631;
            public DXGI_RGB e632;
            public DXGI_RGB e633;
            public DXGI_RGB e634;
            public DXGI_RGB e635;
            public DXGI_RGB e636;
            public DXGI_RGB e637;
            public DXGI_RGB e638;
            public DXGI_RGB e639;
            public DXGI_RGB e640;
            public DXGI_RGB e641;
            public DXGI_RGB e642;
            public DXGI_RGB e643;
            public DXGI_RGB e644;
            public DXGI_RGB e645;
            public DXGI_RGB e646;
            public DXGI_RGB e647;
            public DXGI_RGB e648;
            public DXGI_RGB e649;
            public DXGI_RGB e650;
            public DXGI_RGB e651;
            public DXGI_RGB e652;
            public DXGI_RGB e653;
            public DXGI_RGB e654;
            public DXGI_RGB e655;
            public DXGI_RGB e656;
            public DXGI_RGB e657;
            public DXGI_RGB e658;
            public DXGI_RGB e659;
            public DXGI_RGB e660;
            public DXGI_RGB e661;
            public DXGI_RGB e662;
            public DXGI_RGB e663;
            public DXGI_RGB e664;
            public DXGI_RGB e665;
            public DXGI_RGB e666;
            public DXGI_RGB e667;
            public DXGI_RGB e668;
            public DXGI_RGB e669;
            public DXGI_RGB e670;
            public DXGI_RGB e671;
            public DXGI_RGB e672;
            public DXGI_RGB e673;
            public DXGI_RGB e674;
            public DXGI_RGB e675;
            public DXGI_RGB e676;
            public DXGI_RGB e677;
            public DXGI_RGB e678;
            public DXGI_RGB e679;
            public DXGI_RGB e680;
            public DXGI_RGB e681;
            public DXGI_RGB e682;
            public DXGI_RGB e683;
            public DXGI_RGB e684;
            public DXGI_RGB e685;
            public DXGI_RGB e686;
            public DXGI_RGB e687;
            public DXGI_RGB e688;
            public DXGI_RGB e689;
            public DXGI_RGB e690;
            public DXGI_RGB e691;
            public DXGI_RGB e692;
            public DXGI_RGB e693;
            public DXGI_RGB e694;
            public DXGI_RGB e695;
            public DXGI_RGB e696;
            public DXGI_RGB e697;
            public DXGI_RGB e698;
            public DXGI_RGB e699;
            public DXGI_RGB e700;
            public DXGI_RGB e701;
            public DXGI_RGB e702;
            public DXGI_RGB e703;
            public DXGI_RGB e704;
            public DXGI_RGB e705;
            public DXGI_RGB e706;
            public DXGI_RGB e707;
            public DXGI_RGB e708;
            public DXGI_RGB e709;
            public DXGI_RGB e710;
            public DXGI_RGB e711;
            public DXGI_RGB e712;
            public DXGI_RGB e713;
            public DXGI_RGB e714;
            public DXGI_RGB e715;
            public DXGI_RGB e716;
            public DXGI_RGB e717;
            public DXGI_RGB e718;
            public DXGI_RGB e719;
            public DXGI_RGB e720;
            public DXGI_RGB e721;
            public DXGI_RGB e722;
            public DXGI_RGB e723;
            public DXGI_RGB e724;
            public DXGI_RGB e725;
            public DXGI_RGB e726;
            public DXGI_RGB e727;
            public DXGI_RGB e728;
            public DXGI_RGB e729;
            public DXGI_RGB e730;
            public DXGI_RGB e731;
            public DXGI_RGB e732;
            public DXGI_RGB e733;
            public DXGI_RGB e734;
            public DXGI_RGB e735;
            public DXGI_RGB e736;
            public DXGI_RGB e737;
            public DXGI_RGB e738;
            public DXGI_RGB e739;
            public DXGI_RGB e740;
            public DXGI_RGB e741;
            public DXGI_RGB e742;
            public DXGI_RGB e743;
            public DXGI_RGB e744;
            public DXGI_RGB e745;
            public DXGI_RGB e746;
            public DXGI_RGB e747;
            public DXGI_RGB e748;
            public DXGI_RGB e749;
            public DXGI_RGB e750;
            public DXGI_RGB e751;
            public DXGI_RGB e752;
            public DXGI_RGB e753;
            public DXGI_RGB e754;
            public DXGI_RGB e755;
            public DXGI_RGB e756;
            public DXGI_RGB e757;
            public DXGI_RGB e758;
            public DXGI_RGB e759;
            public DXGI_RGB e760;
            public DXGI_RGB e761;
            public DXGI_RGB e762;
            public DXGI_RGB e763;
            public DXGI_RGB e764;
            public DXGI_RGB e765;
            public DXGI_RGB e766;
            public DXGI_RGB e767;
            public DXGI_RGB e768;
            public DXGI_RGB e769;
            public DXGI_RGB e770;
            public DXGI_RGB e771;
            public DXGI_RGB e772;
            public DXGI_RGB e773;
            public DXGI_RGB e774;
            public DXGI_RGB e775;
            public DXGI_RGB e776;
            public DXGI_RGB e777;
            public DXGI_RGB e778;
            public DXGI_RGB e779;
            public DXGI_RGB e780;
            public DXGI_RGB e781;
            public DXGI_RGB e782;
            public DXGI_RGB e783;
            public DXGI_RGB e784;
            public DXGI_RGB e785;
            public DXGI_RGB e786;
            public DXGI_RGB e787;
            public DXGI_RGB e788;
            public DXGI_RGB e789;
            public DXGI_RGB e790;
            public DXGI_RGB e791;
            public DXGI_RGB e792;
            public DXGI_RGB e793;
            public DXGI_RGB e794;
            public DXGI_RGB e795;
            public DXGI_RGB e796;
            public DXGI_RGB e797;
            public DXGI_RGB e798;
            public DXGI_RGB e799;
            public DXGI_RGB e800;
            public DXGI_RGB e801;
            public DXGI_RGB e802;
            public DXGI_RGB e803;
            public DXGI_RGB e804;
            public DXGI_RGB e805;
            public DXGI_RGB e806;
            public DXGI_RGB e807;
            public DXGI_RGB e808;
            public DXGI_RGB e809;
            public DXGI_RGB e810;
            public DXGI_RGB e811;
            public DXGI_RGB e812;
            public DXGI_RGB e813;
            public DXGI_RGB e814;
            public DXGI_RGB e815;
            public DXGI_RGB e816;
            public DXGI_RGB e817;
            public DXGI_RGB e818;
            public DXGI_RGB e819;
            public DXGI_RGB e820;
            public DXGI_RGB e821;
            public DXGI_RGB e822;
            public DXGI_RGB e823;
            public DXGI_RGB e824;
            public DXGI_RGB e825;
            public DXGI_RGB e826;
            public DXGI_RGB e827;
            public DXGI_RGB e828;
            public DXGI_RGB e829;
            public DXGI_RGB e830;
            public DXGI_RGB e831;
            public DXGI_RGB e832;
            public DXGI_RGB e833;
            public DXGI_RGB e834;
            public DXGI_RGB e835;
            public DXGI_RGB e836;
            public DXGI_RGB e837;
            public DXGI_RGB e838;
            public DXGI_RGB e839;
            public DXGI_RGB e840;
            public DXGI_RGB e841;
            public DXGI_RGB e842;
            public DXGI_RGB e843;
            public DXGI_RGB e844;
            public DXGI_RGB e845;
            public DXGI_RGB e846;
            public DXGI_RGB e847;
            public DXGI_RGB e848;
            public DXGI_RGB e849;
            public DXGI_RGB e850;
            public DXGI_RGB e851;
            public DXGI_RGB e852;
            public DXGI_RGB e853;
            public DXGI_RGB e854;
            public DXGI_RGB e855;
            public DXGI_RGB e856;
            public DXGI_RGB e857;
            public DXGI_RGB e858;
            public DXGI_RGB e859;
            public DXGI_RGB e860;
            public DXGI_RGB e861;
            public DXGI_RGB e862;
            public DXGI_RGB e863;
            public DXGI_RGB e864;
            public DXGI_RGB e865;
            public DXGI_RGB e866;
            public DXGI_RGB e867;
            public DXGI_RGB e868;
            public DXGI_RGB e869;
            public DXGI_RGB e870;
            public DXGI_RGB e871;
            public DXGI_RGB e872;
            public DXGI_RGB e873;
            public DXGI_RGB e874;
            public DXGI_RGB e875;
            public DXGI_RGB e876;
            public DXGI_RGB e877;
            public DXGI_RGB e878;
            public DXGI_RGB e879;
            public DXGI_RGB e880;
            public DXGI_RGB e881;
            public DXGI_RGB e882;
            public DXGI_RGB e883;
            public DXGI_RGB e884;
            public DXGI_RGB e885;
            public DXGI_RGB e886;
            public DXGI_RGB e887;
            public DXGI_RGB e888;
            public DXGI_RGB e889;
            public DXGI_RGB e890;
            public DXGI_RGB e891;
            public DXGI_RGB e892;
            public DXGI_RGB e893;
            public DXGI_RGB e894;
            public DXGI_RGB e895;
            public DXGI_RGB e896;
            public DXGI_RGB e897;
            public DXGI_RGB e898;
            public DXGI_RGB e899;
            public DXGI_RGB e900;
            public DXGI_RGB e901;
            public DXGI_RGB e902;
            public DXGI_RGB e903;
            public DXGI_RGB e904;
            public DXGI_RGB e905;
            public DXGI_RGB e906;
            public DXGI_RGB e907;
            public DXGI_RGB e908;
            public DXGI_RGB e909;
            public DXGI_RGB e910;
            public DXGI_RGB e911;
            public DXGI_RGB e912;
            public DXGI_RGB e913;
            public DXGI_RGB e914;
            public DXGI_RGB e915;
            public DXGI_RGB e916;
            public DXGI_RGB e917;
            public DXGI_RGB e918;
            public DXGI_RGB e919;
            public DXGI_RGB e920;
            public DXGI_RGB e921;
            public DXGI_RGB e922;
            public DXGI_RGB e923;
            public DXGI_RGB e924;
            public DXGI_RGB e925;
            public DXGI_RGB e926;
            public DXGI_RGB e927;
            public DXGI_RGB e928;
            public DXGI_RGB e929;
            public DXGI_RGB e930;
            public DXGI_RGB e931;
            public DXGI_RGB e932;
            public DXGI_RGB e933;
            public DXGI_RGB e934;
            public DXGI_RGB e935;
            public DXGI_RGB e936;
            public DXGI_RGB e937;
            public DXGI_RGB e938;
            public DXGI_RGB e939;
            public DXGI_RGB e940;
            public DXGI_RGB e941;
            public DXGI_RGB e942;
            public DXGI_RGB e943;
            public DXGI_RGB e944;
            public DXGI_RGB e945;
            public DXGI_RGB e946;
            public DXGI_RGB e947;
            public DXGI_RGB e948;
            public DXGI_RGB e949;
            public DXGI_RGB e950;
            public DXGI_RGB e951;
            public DXGI_RGB e952;
            public DXGI_RGB e953;
            public DXGI_RGB e954;
            public DXGI_RGB e955;
            public DXGI_RGB e956;
            public DXGI_RGB e957;
            public DXGI_RGB e958;
            public DXGI_RGB e959;
            public DXGI_RGB e960;
            public DXGI_RGB e961;
            public DXGI_RGB e962;
            public DXGI_RGB e963;
            public DXGI_RGB e964;
            public DXGI_RGB e965;
            public DXGI_RGB e966;
            public DXGI_RGB e967;
            public DXGI_RGB e968;
            public DXGI_RGB e969;
            public DXGI_RGB e970;
            public DXGI_RGB e971;
            public DXGI_RGB e972;
            public DXGI_RGB e973;
            public DXGI_RGB e974;
            public DXGI_RGB e975;
            public DXGI_RGB e976;
            public DXGI_RGB e977;
            public DXGI_RGB e978;
            public DXGI_RGB e979;
            public DXGI_RGB e980;
            public DXGI_RGB e981;
            public DXGI_RGB e982;
            public DXGI_RGB e983;
            public DXGI_RGB e984;
            public DXGI_RGB e985;
            public DXGI_RGB e986;
            public DXGI_RGB e987;
            public DXGI_RGB e988;
            public DXGI_RGB e989;
            public DXGI_RGB e990;
            public DXGI_RGB e991;
            public DXGI_RGB e992;
            public DXGI_RGB e993;
            public DXGI_RGB e994;
            public DXGI_RGB e995;
            public DXGI_RGB e996;
            public DXGI_RGB e997;
            public DXGI_RGB e998;
            public DXGI_RGB e999;
            public DXGI_RGB e1000;
            public DXGI_RGB e1001;
            public DXGI_RGB e1002;
            public DXGI_RGB e1003;
            public DXGI_RGB e1004;
            public DXGI_RGB e1005;
            public DXGI_RGB e1006;
            public DXGI_RGB e1007;
            public DXGI_RGB e1008;
            public DXGI_RGB e1009;
            public DXGI_RGB e1010;
            public DXGI_RGB e1011;
            public DXGI_RGB e1012;
            public DXGI_RGB e1013;
            public DXGI_RGB e1014;
            public DXGI_RGB e1015;
            public DXGI_RGB e1016;
            public DXGI_RGB e1017;
            public DXGI_RGB e1018;
            public DXGI_RGB e1019;
            public DXGI_RGB e1020;
            public DXGI_RGB e1021;
            public DXGI_RGB e1022;
            public DXGI_RGB e1023;
            public DXGI_RGB e1024;
#if NET6_0_OR_GREATER
            public ref DXGI_RGB this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return ref AsSpan()[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<DXGI_RGB> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 1025);
#else
            // there is no way to do this outside of terrible unsafe code. Don't do this in .Net Standard 2.0 

            public DXGI_RGB this[int index]
            {
                get
                {
                    if ((uint)index > 1025)
                        throw new ArgumentOutOfRangeException("index");

                    fixed (DXGI_RGB* basePtr = &e0)
                    {
                        DXGI_RGB* newPtr = basePtr + index;
                        return *newPtr;
                    }
                }
                set
                {
                    if ((uint)index > 1025)
                        throw new ArgumentOutOfRangeException("index");

                    fixed (DXGI_RGB* basePtr = &e0)
                    {
                        DXGI_RGB* newPtr = basePtr + index;
                        *newPtr = value;
                    }
                }
            }
#endif
        }
    }

    internal unsafe struct DEVMODEW
    {
        public fixed ushort dmDeviceName[32];
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public short dmOrientation;
        public short dmPaperSize;
        public short dmPaperLength;
        public short dmPaperWidth;
        public short dmScale;
        public short dmCopies;
        public short dmDefaultSource;
        public short dmPrintQuality;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        public fixed ushort dmFormName[32];
        public short dmUnusedPadding;
        public short dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
    }

    internal unsafe struct DXGI_ADAPTER_DESC
    {
        public fixed ushort Description[128];

        public uint VendorId;

        public uint DeviceId;

        public uint SubSysId;

        public uint Revision;

        public nuint DedicatedVideoMemory;

        public nuint DedicatedSystemMemory;

        public nuint SharedSystemMemory;

        public ulong AdapterLuid;
    }

    internal unsafe struct DXGI_ADAPTER_DESC1
    {
        public fixed ushort Description[128];

        public uint VendorId;

        public uint DeviceId;

        public uint SubSysId;

        public uint Revision;

        public nuint DedicatedVideoMemory;

        public nuint DedicatedSystemMemory;

        public nuint SharedSystemMemory;

        public ulong AdapterLuid;

        public uint Flags;
    }

    internal unsafe struct DXGI_FRAME_STATISTICS
    {
        public uint PresentCount;

        public uint PresentRefreshCount;

        public uint SyncRefreshCount;

        public ulong SyncQPCTime;

        public ulong SyncGPUTime;
    }

    internal unsafe struct DXGI_GAMMA_CONTROL_CAPABILITIES
    {
        public int ScaleAndOffsetSupported;

        public float MaxConvertedValue;

        public float MinConvertedValue;

        public uint NumGammaControlPoints;

        public fixed float ControlPointPositions[1025];
    }

    internal unsafe struct DXGI_MAPPED_RECT
    {
        public int Pitch;

        public byte* pBits;
    }

    internal unsafe partial struct DXGI_MODE_DESC
    {
        public ushort Width;
        public ushort Height;
        public DXGI_RATIONAL RefreshRate;
        public DXGI_FORMAT Format;
        public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;
        public DXGI_MODE_SCALING Scaling;
    }

    internal unsafe partial struct DXGI_OUTPUT_DESC
    {
        internal fixed ushort DeviceName[32];

        internal RECT DesktopCoordinates;

        internal bool AttachedToDesktop;

        internal DXGI_MODE_ROTATION Rotation;

        internal HANDLE Monitor;
    }

    internal unsafe struct DXGI_PRESENT_PARAMETERS
    {
        public uint DirtyRectsCount;

        public RECT* pDirtyRects;

        public RECT* pScrollRect;

        public POINT* pScrollOffset;
    }

    internal unsafe partial struct DXGI_RATIONAL
    {
        public ushort Numerator;
        public ushort Denominator;
    }

    internal partial struct DXGI_RGB
    {
        public float Red;

        public float Green;

        public float Blue;
    }

    internal partial struct DXGI_RGBA
    {
        public float r;

        public float g;

        public float b;

        public float a;
    }

    internal struct DXGI_SAMPLE_DESC
    {
        public uint Count;
        public uint Quality;
    }

    internal unsafe struct DXGI_SURFACE_DESC
    {
        public uint Width;

        public uint Height;

        public DXGI_FORMAT Format;

        public DXGI_SAMPLE_DESC SampleDesc;
    }

    internal unsafe partial struct DXGI_SWAP_CHAIN_DESC
    {
        public DXGI_MODE_DESC BufferDesc;
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint BufferUsage;
        public ushort BufferCount;
        public IntPtr OutputWindow;
        public int Windowed;
        public DXGI_SWAP_EFFECT SwapEffect;
        public ushort Flags;
    }

    internal struct DXGI_SWAP_CHAIN_DESC1
    {
        public uint Width;
        public uint Height;
        public DXGI_FORMAT Format;
        public bool Stereo;
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint BufferUsage;
        public uint BufferCount;
        public DXGI_SCALING Scaling;
        public DXGI_SWAP_EFFECT SwapEffect;
        public DXGI_ALPHA_MODE AlphaMode;
        public uint Flags;
    }

    internal unsafe struct DXGI_SWAP_CHAIN_FULLSCREEN_DESC
    {
        public DXGI_RATIONAL RefreshRate;

        public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;

        public DXGI_MODE_SCALING Scaling;

        public int Windowed;
    }

    internal partial struct D3D11_TEXTURE2D_DESC
    {
        public uint Width;

        public uint Height;

        public uint MipLevels;

        public uint ArraySize;

        public DXGI_FORMAT Format;

        public DXGI_SAMPLE_DESC SampleDesc;

        public D3D11_USAGE Usage;

        public uint BindFlags;

        public uint CPUAccessFlags;

        public uint MiscFlags;
    }
#nullable restore
}
