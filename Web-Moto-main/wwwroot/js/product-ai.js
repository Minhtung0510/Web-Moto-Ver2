window.MotoPresets = {
  brands: ["Honda","Yamaha","Suzuki","Piaggio","Kawasaki","Ducati","Triumph","SYM","KTM"],
  fuels: ["Xăng", "Xăng + Hybrid", "Điện"],
  warranties: ["12 tháng","24 tháng","36 tháng hoặc 30.000 km"],
  badges: ["Hot","New","Sale","Limited"],
  descTemplates: [
    "Thiết kế gọn, động cơ {ENGINE}cc, tiết kiệm {FUEL}. Phù hợp di chuyển nội đô, bảo hành {WAR}.",
    "Mạnh mẽ cho đường dài, tiêu hao thấp, phụ tùng sẵn. Bảo dưỡng dễ dàng tại hệ thống đại lý.",
  ]
};

// đoán từ tên: “Honda Air Blade 160”
function guessFromName(name){
  const out = {};
  const brand = MotoPresets.brands.find(b=>new RegExp(b,"i").test(name));
  if (brand) out.Brand = brand;
  const m = name.match(/(\d{3,4})\b/); // 125/150/160/200/300/1000...
  if (m) out.Engine = m[1];
  return out;
}

function fillQuick(id){
  const $ = s=>document.querySelector(s);
  const name = $('#Name')?.value || '';
  const g = guessFromName(name);
  if (g.Brand) $('#Brand').value = g.Brand;
  if (g.Engine) $('#Engine').value = g.Engine;
  if (!$('#Fuel').value) $('#Fuel').value = MotoPresets.fuels[0];
  if (!$('#Warranty').value) $('#Warranty').value = MotoPresets.warranties[1];
  if (!$('#Badge').value) $('#Badge').value = MotoPresets.badges[0];
  if (!$('#Color').value) $('#Color').value = "Đỏ/Đen/Bạc";
  if (!$('#Stock').value) $('#Stock').value = 50;
  if (!$('#Rating').value) $('#Rating').value = 4.5;
  if (!$('#Description').value){
    const t = MotoPresets.descTemplates[0]
      .replace("{ENGINE}", g.Engine || "—")
      .replace("{FUEL}", $('#Fuel').value || "xăng")
      .replace("{WAR}", $('#Warranty').value || "12 tháng");
    $('#Description').value = t;
  }
}

// tự tính giá khi nhập % giảm hoặc giá cũ
(function priceHelpers(){
  const $ = s=>document.querySelector(s);
  const price = $('#Price'), old = $('#OldPrice'), off = $('#DiscountPercent');
  function clamp(v,min,max){return Math.max(min, Math.min(max, v));}
  function recalc(){
    const p = parseFloat(price.value)||0;
    const o = parseFloat(old.value)||0;
    let d = parseFloat(off.value)||0;

    if (o>0 && p>0 && !off.matches(':focus')) d = clamp(((o-p)/o*100),0,100);
    if (off.matches(':focus') && o>0 && d>0) price.value = Math.round(o*(1-d/100));

    off.value = d ? d.toFixed(0) : "";
  }
  ['input','change'].forEach(ev=>{
    price?.addEventListener(ev,recalc);
    old?.addEventListener(ev,recalc);
    off?.addEventListener(ev,recalc);
  });
})();
