namespace Organo.UI
{
    public static partial class OrganoIcons
    {
        /// <summary>
        /// Ilustrasi anak 2D di Library (390x420) + ikon organ indera. Titik penting tetap sama dengan versi lama
        /// (kepala 195,52 · mulut 195,62 · leher 80–108 · badan 100–296 · kaki 300–410) supaya zoom area,
        /// titik ketuk, peta letak & kuis tempel label tetap pas.
        /// Baju sampai pinggul (±266) dan celana / rok mulai di pinggul (256): organ perut (lambung, hati, usus) seluruhnya
        /// di atas pinggang, tidak tergambar di dalam celana.
        /// Placeholder: shirtFill, shirtStroke, shirtOp, smileOp, headOp, neckOp, chestOp, bellyOp, labelOp,
        /// boyDisplay, girlDisplay, hairOp, bodyAlt.
        /// </summary>
        const string KidBody = @"<svg width='390' height='420' viewBox='0 0 390 420' role='img' aria-label='{{bodyAlt}}'>
<ellipse cx='195' cy='411' rx='70' ry='7' fill='#2E2A4A' opacity='0.08'/>
<g style='display:{{girlDisplay}}'>
  <path d='M156 48C150 84 154 112 168 124L222 124C236 112 240 84 234 48Z' fill='#5A4636' stroke='#3F3126' stroke-width='1.2' opacity='{{hairOp}}'/>
</g>
<!-- kaki, kaus kaki, sepatu -->
<path d='M167 296L188 296L187 386Q187 392 181 392L173 392Q167 392 167 386Z' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<path d='M202 296L223 296L223 386Q223 392 217 392L209 392Q203 392 203 386Z' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<rect x='167' y='372' width='20.5' height='14' rx='4' fill='#FFFFFF' stroke='#E4E2EC' stroke-width='1.2'/>
<rect x='202.5' y='372' width='20.5' height='14' rx='4' fill='#FFFFFF' stroke='#E4E2EC' stroke-width='1.2'/>
<path d='M167.5 377H187M203 377H222.5' stroke='{{shirtStroke}}' stroke-width='2' opacity='0.7'/>
<g style='display:{{boyDisplay}}'>
  <path d='M158 403Q158 389 171 389L184 389Q192 389 192 397L192 404Q192 409 187 409L163 409Q158 409 158 403Z' fill='#4F5D9C' stroke='#3B4677' stroke-width='1.2'/>
  <path d='M198 397Q198 389 206 389L219 389Q232 389 232 403Q232 409 227 409L203 409Q198 409 198 404Z' fill='#4F5D9C' stroke='#3B4677' stroke-width='1.2'/>
  <path d='M162 404H190M200 404H228' stroke='#FFFFFF' stroke-width='2.2' opacity='0.85'/>
</g>
<g style='display:{{girlDisplay}}'>
  <path d='M158 403Q158 389 171 389L184 389Q192 389 192 397L192 404Q192 409 187 409L163 409Q158 409 158 403Z' fill='#E07A98' stroke='#C2577A' stroke-width='1.2'/>
  <path d='M198 397Q198 389 206 389L219 389Q232 389 232 403Q232 409 227 409L203 409Q198 409 198 404Z' fill='#E07A98' stroke='#C2577A' stroke-width='1.2'/>
  <circle cx='177' cy='393' r='2.4' fill='#FFFFFF'/><circle cx='213' cy='393' r='2.4' fill='#FFFFFF'/>
</g>
<!-- lengan menempel di bahu, sedikit menekuk, tangan di samping pinggul -->
<path d='M151 118Q137 178 129 234' stroke='#D49A72' stroke-width='22' fill='none' stroke-linecap='round'/>
<path d='M151 118Q137 178 129 234' stroke='#F2C4A0' stroke-width='19' fill='none' stroke-linecap='round'/>
<path d='M239 118Q253 178 261 234' stroke='#D49A72' stroke-width='22' fill='none' stroke-linecap='round'/>
<path d='M239 118Q253 178 261 234' stroke='#F2C4A0' stroke-width='19' fill='none' stroke-linecap='round'/>
<circle cx='127' cy='244' r='12' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<circle cx='263' cy='244' r='12' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<path d='M120 242Q123 249 130 250M270 242Q267 249 260 250' stroke='#D49A72' stroke-width='1.3' fill='none' stroke-linecap='round'/>
<!-- leher -->
<rect x='184' y='80' width='22' height='26' rx='8' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<ellipse cx='195' cy='90' rx='11' ry='3.5' fill='#D49A72' opacity='0.35'/>
<!-- badan (kulit, terlihat saat baju dipudarkan) -->
<path d='M150 106Q172 98 195 98Q218 98 240 106L246 160Q244 210 238 236L239.5 282L150.5 282L152 236Q146 210 144 160Z' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<!-- celana pendek / rok (pinggang tertutup ujung baju) -->
<g style='display:{{boyDisplay}}'>
  <!-- celana pendek: pinggang tertutup ujung baju, dua pipa kaki dengan selangkangan rapat & lipatan ujung -->
  <path d='M149 256L241 256L244.5 307Q244.8 312 240 312L201 312Q197.4 312 197 308.5L195 292L193 308.5Q192.6 312 189 312L150 312Q145.2 312 145.5 307Z' fill='#FFF3D6' stroke='#D9C597' stroke-width='1.5' stroke-linejoin='round'/>
  <path d='M195 292L194.2 301M195 292L195.8 301' stroke='#E8D9B2' stroke-width='1.2' stroke-linecap='round'/>
  <path d='M146.3 303.5H192.6M197.4 303.5H243.7' stroke='#E3D2A6' stroke-width='1.6' stroke-linecap='round'/>
  <path d='M195 271V280Q195 285 200 286' stroke='#E0CFA3' stroke-width='1.4' fill='none' stroke-linecap='round'/>
  <path d='M156 273Q162 280 158 289M234 273Q228 280 232 289' stroke='#E0CFA3' stroke-width='1.4' fill='none' stroke-linecap='round'/>
  <path d='M149 256L241 256L241.5 270Q195 279 148.5 270Z' fill='#2E2A4A' opacity='0.04'/>
  <path d='M180 296Q188 298 193 308.5M210 296Q202 298 197 308.5' stroke='#2E2A4A' stroke-width='5' opacity='0.035' fill='none' stroke-linecap='round'/>
</g>
<g style='display:{{girlDisplay}}'>
  <path d='M151 256L239 256L258 318Q260 324 254 324L136 324Q130 324 132 318Z' fill='#B7A4F2' stroke='#8B6FE8' stroke-width='1.5' stroke-linejoin='round'/>
  <path d='M174 270L166 322M195 270V322M216 270L224 322' stroke='#9C86EE' stroke-width='1.5'/>
  <path d='M135 316H255' stroke='#FFFFFF' stroke-width='3' opacity='0.6'/>
</g>
<!-- organ samar: leher (tenggorokan & kerongkongan) -->
<g style='opacity:{{neckOp}};transition:opacity .45s' stroke-linecap='round' fill='none'>
  <path d='M200 84V150C200 170 203 180 206 192' stroke='#12857A' stroke-width='8'/>
  <path d='M200 84V150C200 170 203 180 206 192' stroke='#9CE6DC' stroke-width='3'/>
  <path d='M189 90V132' stroke='#6F52D1' stroke-width='9'/>
  <path d='M184.5 98H193.5M184.5 106H193.5M184.5 114H193.5M184.5 122H193.5' stroke='#E8E2FB' stroke-width='2.2'/>
  <ellipse cx='189' cy='88' rx='7' ry='5.5' fill='#CDBFF5' stroke='#8B6FE8' stroke-width='1.4'/>
</g>
<!-- organ samar: perut -->
<g style='opacity:{{bellyOp}};transition:opacity .45s' stroke-linecap='round' stroke-linejoin='round' fill='none'>
  <!-- usus besar membingkai usus halus; semuanya berakhir di atas pinggang celana (256) -->
  <path d='M156 254V231Q156 220 168 220H222Q234 220 234 231V254' stroke='#F2B705' stroke-width='11'/>
  <path d='M156 254V231Q156 220 168 220H222Q234 220 234 231V254' stroke='#FBE08A' stroke-width='3.5'/>
  <path d='M170 229H214C219 229 219 236.5 214 236.5H176C171 236.5 171 244 176 244H214C219 244 219 251.5 214 251.5H172' stroke='#EF476F' stroke-width='5'/>
  <path d='M160 198C160 184 176 180 192 184C200 186 204 192 200 198C194 206 168 210 160 198Z' fill='#C8414B' stroke='#A3333C' stroke-width='1.2' opacity='0.9'/>
  <g transform='translate(186 172) scale(0.34)'><path d='M50 8H74V30C74 42 104 40 122 56C152 78 148 122 108 130C70 136 38 120 36 88C35 62 50 54 50 38Z' fill='#F58CA3' stroke='#D9607E' stroke-width='4'/><path d='M50 8H74V30C74 42 104 40 122 56C140 70 142 96 134 108C128 80 100 62 66 56C56 52 50 46 50 38Z' fill='#F9B0C0' stroke='none'/></g>
</g>
<!-- organ samar: dada -->
<g style='opacity:{{chestOp}};transition:opacity .45s'>
  <path d='M195 104V132' stroke='#5C45C2' stroke-width='5' stroke-linecap='round'/>
  <g transform='translate(133 91.6) scale(3.4)'><path d='M16 6v10c0 6-3 12-8 12-3 0-3-4-3-7 0-6 3-12 6-14 2-1.5 5-2 5-1z' fill='#8B6FE8' stroke='#5C45C2' stroke-width='0.35'/><path d='M20 6v10c0 6 3 12 8 12 3 0 3-4 3-7 0-6-3-12-6-14-2-1.5-5-2-5-1z' fill='#A892F0' stroke='#5C45C2' stroke-width='0.35'/></g>
  <g transform='translate(190 133) scale(2)'><path d='M12 21C5 15 3 12 3 8.5A4.5 4.5 0 0 1 12 6a4.5 4.5 0 0 1 9 2.5C21 12 19 15 12 21z' fill='#EF476F' stroke='#C93758' stroke-width='0.4'/></g>
</g>
<!-- baju: berakhir di pinggang dengan ujung membulat -->
<g style='opacity:{{shirtOp}};transition:opacity .45s' stroke-linejoin='round'>
  <path d='M148 108Q170 99 195 99Q220 99 242 108L250 158Q248 214 243 262Q219 271 195 271Q171 271 147 262Q142 214 140 158Z' fill='{{shirtFill}}' stroke='{{shirtStroke}}' stroke-width='1.5'/>
  <path d='M150 108Q130 116 124 150Q136 160 149 156L155 128Z' fill='{{shirtFill}}' stroke='{{shirtStroke}}' stroke-width='1.5'/>
  <path d='M240 108Q260 116 266 150Q254 160 241 156L235 128Z' fill='{{shirtFill}}' stroke='{{shirtStroke}}' stroke-width='1.5'/>
  <path d='M126 145Q137 152 150 150M264 145Q253 152 240 150' stroke='{{shirtStroke}}' stroke-width='2' fill='none' stroke-linecap='round'/>
  <path d='M224 104Q238 108 242 112L250 158Q248 214 243 262Q236 266 229 267Q236 210 232 150Z' fill='#2E2A4A' opacity='0.05'/>
  <path d='M180 101Q195 116 210 101' stroke='{{shirtStroke}}' stroke-width='3.5' fill='none' stroke-linecap='round'/>
  <path d='M150 257Q195 273 240 257' stroke='{{shirtStroke}}' stroke-width='1.5' fill='none' opacity='0.5'/>
</g>
<!-- kepala -->
<circle cx='158' cy='56' r='8' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<circle cx='232' cy='56' r='8' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<path d='M156 52Q159 56 156 60M234 52Q231 56 234 60' stroke='#D49A72' stroke-width='1.3' fill='none' stroke-linecap='round'/>
<circle cx='195' cy='52' r='36' fill='#F2C4A0' stroke='#D49A72' stroke-width='1.5'/>
<path d='M166 72Q195 96 224 72Q216 87 195 88Q174 87 166 72Z' fill='#D49A72' opacity='0.16'/>
<g opacity='{{hairOp}}'>
  <g style='display:{{boyDisplay}}'>
    <path d='M158 52C154 22 174 10 196 10C220 10 238 24 232 52C228 42 222 36 214 33C210 39 200 41 192 37C185 43 172 46 158 52Z' fill='#5A4636' stroke='#3F3126' stroke-width='1.2' stroke-linejoin='round'/>
    <path d='M190 10Q197 1 206 4Q199 6 198 11Z' fill='#5A4636' stroke='#3F3126' stroke-width='1'/>
    <path d='M175 20Q185 15 196 16' stroke='#7A6450' stroke-width='2.6' fill='none' stroke-linecap='round'/>
  </g>
  <g style='display:{{girlDisplay}}'>
    <path d='M157 56C152 22 174 8 196 8C221 8 240 24 233 56C228 42 219 34 207 29C199 38 179 43 157 56Z' fill='#5A4636' stroke='#3F3126' stroke-width='1.2' stroke-linejoin='round'/>
    <path d='M176 18Q186 13 197 14' stroke='#7A6450' stroke-width='2.6' fill='none' stroke-linecap='round'/>
    <path d='M226 22l11-8v16zM226 22l-11-8v16z' fill='#EF476F' stroke='#C2577A' stroke-width='1'/>
    <circle cx='226' cy='22' r='3.2' fill='#D6385E'/>
  </g>
</g>
<path d='M177 36Q183 32.5 189 35.5M201 35.5Q207 32.5 213 36' stroke='#5A4636' stroke-width='2' fill='none' stroke-linecap='round'/>
<ellipse cx='183' cy='46' rx='3.8' ry='4.6' fill='#2E2A4A'/>
<ellipse cx='207' cy='46' rx='3.8' ry='4.6' fill='#2E2A4A'/>
<circle cx='184.4' cy='44.3' r='1.4' fill='#FFFFFF'/><circle cx='208.4' cy='44.3' r='1.4' fill='#FFFFFF'/>
<path d='M192.5 53Q195 56 197.5 53' stroke='#C98B62' stroke-width='1.7' fill='none' stroke-linecap='round'/>
<ellipse cx='171' cy='58' rx='6.5' ry='3.8' fill='#FF8A7A' opacity='0.38'/>
<ellipse cx='219' cy='58' rx='6.5' ry='3.8' fill='#FF8A7A' opacity='0.38'/>
<path d='M186 62Q195 70 204 62' stroke='#2E2A4A' stroke-width='2' fill='none' stroke-linecap='round' style='opacity:{{smileOp}};transition:opacity .3s'/>
<!-- organ samar: kepala (otak, mata, hidung, mulut) -->
<g style='opacity:{{headOp}};transition:opacity .45s'>
  <g transform='translate(170 9) scale(0.5)'>
    <path d='M50 8C30 0 10 10 10 30C2 36 4 56 18 60C26 70 42 68 50 62C58 68 74 70 82 60C96 56 98 36 90 30C90 10 70 0 50 8Z' fill='#F5B7C6' stroke='#E07A98' stroke-width='3'/>
    <path d='M50 10V60M22 26C30 28 34 22 40 28M18 44C26 40 32 48 40 44M78 26C70 28 66 22 60 28M82 44C74 40 68 48 60 44' stroke='#E07A98' stroke-width='3' fill='none' stroke-linecap='round'/>
  </g>
  <circle cx='183' cy='46' r='7.5' stroke='#2B7BD6' stroke-width='1.6' fill='none' opacity='0.8'/>
  <circle cx='207' cy='46' r='7.5' stroke='#2B7BD6' stroke-width='1.6' fill='none' opacity='0.8'/>
  <circle cx='195' cy='54' r='5' stroke='#F2994A' stroke-width='1.6' fill='none' opacity='0.8'/>
  <path d='M181 61c4-6 9-6 14-3 5-3 10-3 14 3-3 8-7 11-14 11s-11-3-14-11z' fill='#EF476F' stroke='#C93758' stroke-width='1'/>
  <rect x='185' y='59' width='20' height='4.5' rx='2' fill='#FFFFFF'/>
  <path d='M187 68c4-3 12-3 16 0' stroke='#F9B0C0' stroke-width='3' fill='none' stroke-linecap='round'/>
</g>
</svg>";

        // ---------------------------------------------------------------- organ indera & tenggorokan

        // Mata: bola mata putih, iris biru, pupil, kilau, sedikit saraf mata di belakang
        const string EyeBall =
            "<path d='M52 40c4 2 7 6 9 12' stroke='#F2C46B' stroke-width='5' fill='none' stroke-linecap='round'/>" +
            "<circle cx='30' cy='32' r='22' fill='#FFFFFF' stroke='#B9C3D6' stroke-width='1.6'/>" +
            "<path d='M14 22c3-5 8-8 13-9' stroke='#E4E9F2' stroke-width='3' fill='none' stroke-linecap='round'/>" +
            "<circle cx='30' cy='32' r='10.5' fill='#4D9DE0' stroke='#2B7BD6' stroke-width='1.4'/>" +
            "<path d='M30 22.5v4M30 37.5v4M20.5 32h4M35.5 32h4' stroke='#7DB9EA' stroke-width='1.4' stroke-linecap='round'/>" +
            "<circle cx='30' cy='32' r='5' fill='#1F1D33'/>" +
            "<circle cx='27' cy='29' r='2' fill='#FFFFFF'/>";
        const string EyeLid =
            "<path d='M6 32C12 18 22 12 30 12S48 18 54 32' stroke='#8B6FE8' stroke-width='2.4' fill='none' stroke-linecap='round'/>" +
            "<path d='M12 21l-4-4M20 15l-2-5M30 12V7M40 15l2-5M48 21l4-4' stroke='#5C45C2' stroke-width='2' stroke-linecap='round'/>";

        // Hidung: tampak depan, batang hidung & dua lubang hidung
        const string Nose =
            "<path d='M26 8C26 22 18 32 14 40C10 47 14 54 22 54C25 58 39 58 42 54C50 54 54 47 50 40C46 32 38 22 38 8Z' fill='#F2B89A' stroke='#D18A66' stroke-width='1.6' stroke-linejoin='round'/>" +
            "<path d='M30 12C30 24 27 34 24 40' stroke='#F9D2BE' stroke-width='3' fill='none' stroke-linecap='round'/>" +
            "<ellipse cx='25' cy='49' rx='4.5' ry='3' fill='#B5694A'/><ellipse cx='39' cy='49' rx='4.5' ry='3' fill='#B5694A'/>" +
            "<path d='M8 30c-3 3-4 7-2 10M56 30c3 3 4 7 2 10' stroke='#9CE6DC' stroke-width='2.2' fill='none' stroke-linecap='round'/>";

        // Tenggorokan: faring, epiglotis, laring (kotak suara) & trakea bercincin, kerongkongan di belakang
        const string Throat =
            "<path d='M40 6V58' stroke='#F0A85C' stroke-width='9' stroke-linecap='round'/>" +
            "<path d='M26 4C26 4 22 10 26 16' stroke='#E98F86' stroke-width='9' fill='none' stroke-linecap='round'/>" +
            "<path d='M26 30V60' stroke='#8B6FE8' stroke-width='13' stroke-linecap='round'/>" +
            "<path d='M19.5 36h13M19.5 43h13M19.5 50h13M19.5 57h13' stroke='#E8E2FB' stroke-width='2.4' stroke-linecap='round'/>" +
            "<path d='M15 18h22l-3 14H18Z' fill='#CDBFF5' stroke='#6F52D1' stroke-width='1.5' stroke-linejoin='round'/>" +
            "<path d='M22 18c0-5 2-8 6-9' stroke='#F29BAE' stroke-width='3' fill='none' stroke-linecap='round'/>";

        // Lambang laki-laki (lingkaran + panah) dan perempuan (lingkaran + tanda tambah), gaya organ Organo
        const string MaleSign =
            "<path d='M37 27L52 12M41 12H52V23' stroke='#3F5FC4' stroke-width='7' fill='none' stroke-linecap='round' stroke-linejoin='round'/>" +
            "<path d='M37 27L52 12M41 12H52V23' stroke='#7F9BF0' stroke-width='3' fill='none' stroke-linecap='round' stroke-linejoin='round'/>" +
            "<circle cx='26.5' cy='39.5' r='15' fill='#E6EBFF' stroke='#3F5FC4' stroke-width='3.4'/>" +
            "<path d='M17 33q3-6 9-7' stroke='#FFFFFF' stroke-width='3' fill='none' stroke-linecap='round'/>";
        const string FemaleSign =
            "<path d='M32 41V58M24 50.5H40' stroke='#C2477A' stroke-width='7' fill='none' stroke-linecap='round'/>" +
            "<path d='M32 41V58M24 50.5H40' stroke='#F29BBA' stroke-width='3' fill='none' stroke-linecap='round'/>" +
            "<circle cx='32' cy='25' r='15' fill='#FDE3EC' stroke='#C2477A' stroke-width='3.4'/>" +
            "<path d='M22.5 19q3-6 9-7' stroke='#FFFFFF' stroke-width='3' fill='none' stroke-linecap='round'/>";

        // Tulang klasik (dua bonggol di tiap ujung): garis luar tebal digambar dulu, lalu isinya, supaya garisnya rapi
        const string BoneShapes = "<rect x='17' y='26' width='30' height='12' rx='3'/><circle cx='17' cy='25' r='7'/><circle cx='17' cy='39' r='7'/><circle cx='47' cy='25' r='7'/><circle cx='47' cy='39' r='7'/>";
        const string BoneCore =
            "<g fill='#A9864E' stroke='#A9864E' stroke-width='5' stroke-linejoin='round'>" + BoneShapes + "</g>" +
            "<g fill='#FBF3E2'>" + BoneShapes + "</g>" +
            "<path d='M20 29.5H44' stroke='#FFFFFF' stroke-width='2.6' stroke-linecap='round'/>";
        const string BoneShape = "<g transform='rotate(-35 32 32)'>" + BoneCore + "</g>";

        static void RegisterKidAndSenses()
        {
            Svg["body_template"] = KidBody;

            // Gambar pilih gender: lengannya digambar sampai y=200, sedangkan viewBox aslinya berhenti di y=160,
            // sehingga gambar meluber ke bawah kotak dan menimpa tulisan "Laki-laki"/"Perempuan". ViewBox diperluas.
            foreach (var k in new[] { "gender_boy", "gender_girl" })
                if (Svg.TryGetValue(k, out var g))
                    Svg[k] = g.Replace("width=\"118\" height=\"112\" viewBox=\"115 8 160 152\"", "width=\"120\" height=\"153\" viewBox=\"115 8 160 204\"");

            Svg["organ_mata"] = "<svg viewBox='0 0 64 64'>" + EyeBall + EyeLid +
                "<path d='M24 58q6 3 12 0' stroke='#2E2A4A' stroke-width='1.6' fill='none' stroke-linecap='round'/></svg>";
            Svg["mini_mata"] = "<svg viewBox='0 0 64 64'>" + EyeBall + "</svg>";

            Svg["organ_hidung"] = "<svg viewBox='0 0 64 64'>" + Nose +
                "<circle cx='27' cy='24' r='2' fill='#2E2A4A'/><circle cx='37' cy='24' r='2' fill='#2E2A4A'/>" +
                "<path d='M29 30q3 2.5 6 0' stroke='#2E2A4A' stroke-width='1.5' fill='none' stroke-linecap='round'/></svg>";
            Svg["mini_hidung"] = "<svg viewBox='0 0 64 64'>" + Nose + "</svg>";

            Svg["organ_tenggorokan"] = "<svg viewBox='0 0 64 64'>" + Throat +
                "<circle cx='22.5' cy='24' r='1.8' fill='#2E2A4A'/><circle cx='29.5' cy='24' r='1.8' fill='#2E2A4A'/>" +
                "<path d='M23.5 28q2.5 2 5 0' stroke='#2E2A4A' stroke-width='1.4' fill='none' stroke-linecap='round'/></svg>";
            Svg["mini_tenggorokan"] = "<svg viewBox='0 0 64 64'>" + Throat + "</svg>";

            // organ reproduksi: ikon memakai lambang laki-laki / perempuan (ramah anak), model 3D-nya yang menunjukkan organ
            Svg["organ_reprolaki"] = "<svg viewBox='0 0 64 64'>" + MaleSign +
                "<circle cx='22' cy='38' r='2' fill='#2E2A4A'/><circle cx='31' cy='38' r='2' fill='#2E2A4A'/>" +
                "<path d='M23 44q3.5 3 7 0' stroke='#2E2A4A' stroke-width='1.6' fill='none' stroke-linecap='round'/></svg>";
            Svg["mini_reprolaki"] = "<svg viewBox='0 0 64 64'>" + MaleSign + "</svg>";
            Svg["organ_reproperempuan"] = "<svg viewBox='0 0 64 64'>" + FemaleSign +
                "<circle cx='27.5' cy='23' r='2' fill='#2E2A4A'/><circle cx='36.5' cy='23' r='2' fill='#2E2A4A'/>" +
                "<path d='M28.5 29q3.5 3 7 0' stroke='#2E2A4A' stroke-width='1.6' fill='none' stroke-linecap='round'/></svg>";
            Svg["mini_reproperempuan"] = "<svg viewBox='0 0 64 64'>" + FemaleSign + "</svg>";
            // rangka: tulang klasik yang lucu (bukan tengkorak, supaya tidak menyeramkan)
            Svg["organ_rangka"] = "<svg viewBox='0 0 64 64'><g transform='rotate(-35 32 32)'>" + BoneCore +
                "<circle cx='27' cy='31' r='2' fill='#2E2A4A'/><circle cx='37' cy='31' r='2' fill='#2E2A4A'/>" +
                "<path d='M28.5 34.5q3.5 3 7 0' stroke='#2E2A4A' stroke-width='1.6' fill='none' stroke-linecap='round'/>" +
                "<ellipse cx='22.5' cy='34' rx='2.4' ry='1.4' fill='#F5A3A8' opacity='0.75'/><ellipse cx='41.5' cy='34' rx='2.4' ry='1.4' fill='#F5A3A8' opacity='0.75'/></g></svg>";
            Svg["mini_rangka"] = "<svg viewBox='0 0 64 64'>" + BoneShape + "</svg>";
            Svg["cat_gerak"] = "<svg viewBox='0 0 24 24'><path d='M6.5 4.2a2.4 2.4 0 0 0-2.3 3.1 2.4 2.4 0 0 0 1.4 4L13 18.6a2.4 2.4 0 0 0 4 1.4 2.4 2.4 0 1 0 2.7-3.5 2.4 2.4 0 0 0-1.4-4L11 5.2a2.4 2.4 0 0 0-4.5-1z' fill='#FBF3E2' stroke='#8A6D3B' stroke-width='1.6' stroke-linejoin='round'/></svg>";
            Svg["bone"] = "<svg viewBox='0 0 24 24'><path d='M6.5 4.2a2.4 2.4 0 0 0-2.3 3.1 2.4 2.4 0 0 0 1.4 4L13 18.6a2.4 2.4 0 0 0 4 1.4 2.4 2.4 0 1 0 2.7-3.5 2.4 2.4 0 0 0-1.4-4L11 5.2a2.4 2.4 0 0 0-4.5-1z' fill='none' stroke='#2E2A4A' stroke-width='1.8' stroke-linejoin='round'/></svg>";
            Svg["cat_repro"] ="<svg viewBox='0 0 24 24'>" +
                "<circle cx='8.5' cy='8.5' r='4.6' fill='#FDE3EC' stroke='#E0678F' stroke-width='1.8'/><path d='M8.5 13.1V20M5.5 17H11.5' stroke='#E0678F' stroke-width='1.8' stroke-linecap='round'/>" +
                "<circle cx='15' cy='14.5' r='4.6' fill='#E6EBFF' stroke='#5B7BE0' stroke-width='1.8'/><path d='M18.3 11.2L21.5 8M17.6 8H21.5V11.9' stroke='#5B7BE0' stroke-width='1.8' fill='none' stroke-linecap='round' stroke-linejoin='round'/></svg>";

            Svg["cat_indera"] = "<svg viewBox='0 0 24 24'><path d='M2 12C5 6 9 4 12 4s7 2 10 8c-3 6-7 8-10 8S5 18 2 12Z' fill='#FFFFFF' stroke='#2B7BD6' stroke-width='1.6'/><circle cx='12' cy='12' r='4' fill='#4D9DE0'/><circle cx='12' cy='12' r='1.8' fill='#1F1D33'/></svg>";
        }
    }
}
