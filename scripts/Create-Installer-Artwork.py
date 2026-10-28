from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
out = root / "Kurulum" / "Assets"
out.mkdir(exist_ok=True)
fonts = Path("C:/Windows/Fonts")
def font(size, bold=False):
    return ImageFont.truetype(str(fonts / ("segoeuib.ttf" if bold else "segoeui.ttf")), size)
def surface(w, h):
    image = Image.new("RGB", (w,h))
    draw = ImageDraw.Draw(image)
    for y in range(h):
        t = y/max(1,h-1)
        draw.line((0,y,w,y), fill=(int(14-9*t),int(49-25*t),int(78-31*t)))
    return image
icon = Image.open(root / "DogruFatura.Wpf/Assets/AppIcon_512.png").convert("RGBA")
side = surface(328,628); d = ImageDraw.Draw(side)
d.arc((-105,-160,470,415), 5, 132, fill="#34566E", width=2)
d.arc((-160,405,500,1065), 180, 320, fill="#CDA34F", width=2)
side.paste(icon.resize((156,156),Image.Resampling.LANCZOS),(86,58),icon.resize((156,156),Image.Resampling.LANCZOS))
d.text((164,245),"DoğruFatura",font=font(38,True),fill="#FFD36F",anchor="mm")
for i,line in enumerate(["Düzenli kayıtlar.","Güvenli arşiv.","Güçlü çalışma alanı."]):
    d.text((164,301+29*i),line,font=font(18),fill="#D3E2EE",anchor="mm")
d.line((52,421,276,421), fill="#55728A",width=1)
for i,(number,label) in enumerate([("01","HAZIRLIK"),("02","KURULUM"),("03","TAMAMLAMA")]):
    y=454+i*43
    d.rounded_rectangle((52,y-12,84,y+15),radius=8,outline="#CDA34F",width=1)
    d.text((68,y+1),number,font=font(14,True),fill="#FFD36F",anchor="mm")
    d.text((100,y+1),label,font=font(15,True),fill="#EAF1F8",anchor="lm")
side.save(out/"wizard.bmp")
header=surface(300,114);d=ImageDraw.Draw(header)
mini=icon.resize((70,70),Image.Resampling.LANCZOS);header.paste(mini,(10,22),mini)
d.text((92,48),"DoğruFatura",font=font(26,True),fill="#FFD36F",anchor="lm")
d.text((94,78),"KURULUM SİHİRBAZI",font=font(12),fill="#D3E2EE",anchor="lm")
header.save(out/"header.bmp")
