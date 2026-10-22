import{H as N,K as T,o as n,c as a,J as y,b as e,_ as O,s as S,C as B,d as C,i as p,t as o,e as d,O as u,F as b,x as m,j as F,q as U,r as v,l as $,n as h}from"./index-ZHDJ9xz-.js";import{_ as D}from"./EnTetePage-DcFmNf5c.js";import{s as E}from"./index-boRXOPyP.js";import{s as k}from"./index-Btqx_4cc.js";import"./BoutonRetour-4Iurk3Pr.js";var q=`
    .p-progressspinner {
        position: relative;
        margin: 0 auto;
        width: 100px;
        height: 100px;
        display: inline-block;
    }

    .p-progressspinner::before {
        content: '';
        display: block;
        padding-top: 100%;
    }

    .p-progressspinner-spin {
        height: 100%;
        transform-origin: center center;
        width: 100%;
        position: absolute;
        top: 0;
        bottom: 0;
        left: 0;
        right: 0;
        margin: auto;
        animation: p-progressspinner-rotate 2s linear infinite;
    }

    .p-progressspinner-circle {
        stroke-dasharray: 89, 200;
        stroke-dashoffset: 0;
        stroke: dt('progressspinner.colorOne');
        animation:
            p-progressspinner-dash 1.5s ease-in-out infinite,
            p-progressspinner-color 6s ease-in-out infinite;
        stroke-linecap: round;
    }

    @keyframes p-progressspinner-rotate {
        100% {
            transform: rotate(360deg);
        }
    }
    @keyframes p-progressspinner-dash {
        0% {
            stroke-dasharray: 1, 200;
            stroke-dashoffset: 0;
        }
        50% {
            stroke-dasharray: 89, 200;
            stroke-dashoffset: -35px;
        }
        100% {
            stroke-dasharray: 89, 200;
            stroke-dashoffset: -124px;
        }
    }
    @keyframes p-progressspinner-color {
        100%,
        0% {
            stroke: dt('progressspinner.color.one');
        }
        40% {
            stroke: dt('progressspinner.color.two');
        }
        66% {
            stroke: dt('progressspinner.color.three');
        }
        80%,
        90% {
            stroke: dt('progressspinner.color.four');
        }
    }
`,L={root:"p-progressspinner",spin:"p-progressspinner-spin",circle:"p-progressspinner-circle"},M=N.extend({name:"progressspinner",style:q,classes:L}),j={name:"BaseProgressSpinner",extends:T,props:{strokeWidth:{type:String,default:"2"},fill:{type:String,default:"none"},animationDuration:{type:String,default:"2s"}},style:M,provide:function(){return{$pcProgressSpinner:this,$parentInstance:this}}},P={name:"ProgressSpinner",extends:j,inheritAttrs:!1,computed:{svgStyle:function(){return{"animation-duration":this.animationDuration}}}},W=["fill","stroke-width"];function z(i,c,w,g,f,s){return n(),a("div",y({class:i.cx("root"),role:"progressbar"},i.ptmi("root")),[(n(),a("svg",y({class:i.cx("spin"),viewBox:"25 25 50 50",style:s.svgStyle},i.ptm("spin")),[e("circle",y({class:i.cx("circle"),cx:"50",cy:"50",r:"20",fill:i.fill,"stroke-width":i.strokeWidth,strokeMiterlimit:"10"},i.ptm("circle")),null,16,W)],16))],16)}P.render=z;const G={class:"p-6 min-h-screen bg-slate-50 font-sans"},H={key:0,class:"flex justify-center p-12"},J={key:1,class:"bg-red-50 text-red-600 p-4 rounded-lg flex items-center gap-3"},K={key:2,class:"space-y-8 mt-6"},Q={class:"border rounded-md overflow-hidden shadow-sm bg-white"},X={class:"p-6 grid grid-cols-1 md:grid-cols-3 gap-6 items-center text-sm"},Y={class:"mt-3"},Z={class:"mt-3 flex items-center gap-2"},ee={key:1,class:"text-gray-800 font-medium"},te={class:"text-gray-800 font-medium"},se={class:"mt-3"},re=["value"],ne={key:1,class:"text-gray-800 font-medium ml-2"},oe={class:"mt-3"},ae={class:"text-gray-800"},le={class:"text-gray-800"},ie={class:"mt-3"},de=["value"],pe={key:1,class:"text-gray-800 font-medium ml-2"},ue={class:"border rounded-md overflow-hidden shadow-sm bg-white"},ce={class:"p-6 grid grid-cols-1 md:grid-cols-4 gap-8 items-end"},ge={class:"flex gap-1 bg-gray-50 p-1 rounded border border-gray-200"},fe={class:"flex gap-1 bg-gray-50 p-1 rounded border border-gray-200"},xe={class:"flex gap-1 bg-gray-50 p-1 rounded border border-gray-200"},ye={class:"flex items-center justify-center py-[7px] text-[13px] font-bold border border-green-200 rounded text-gray-700 bg-white shadow-sm w-[120px]"},be={class:"border rounded-md overflow-hidden shadow-sm bg-white"},me={class:"overflow-x-auto"},ve={class:"w-full text-center border-collapse"},he={class:"text-[13px] font-bold text-gray-700"},ke={class:"py-5 px-2 border-r border-gray-200"},we={class:"py-2 px-2 border-r border-gray-200"},_e={key:1,class:"text-gray-800 font-bold text-sm"},Ce={class:"py-2 px-2 border-r border-gray-200"},Ee={key:1,class:"text-gray-800 font-bold text-sm"},Pe={class:"py-2 px-2 border-r border-gray-200"},Ve={key:1,class:"text-gray-800 font-bold text-sm"},Ie={class:"py-2 px-2 border-r border-gray-200"},Ae={key:1,class:"text-gray-800 font-bold text-sm"},Re={class:"py-2 px-2 border-r border-gray-200"},Ne={class:"font-bold text-[#059669] text-base"},Te={class:"py-2 px-2"},Oe={class:"font-bold text-[#dc2626] text-base"},Se={__name:"ExecEchantillonnageView",setup(i){const c=U(),w=$(),g=c.params.execControleOfId,f=c.query.posteCode||"",s=v(null),_=v(!0),x=v("");S(async()=>{try{const l=await B.get(`/ExecEchantillonnage/${g}`,{params:{posteCode:f||void 0}});s.value=l.data}catch(l){console.error("Erreur chargement:",l),x.value="Erreur lors du chargement des données."}finally{_.value=!1}});const V=()=>{w.push({name:"operateur-of-fini",query:{execControleOfId:g,posteCode:f,equipe:c.query.equipe||void 0}})},I=l=>s.value?.niveauControle?s.value.niveauControle.toUpperCase()===l:!1,A=l=>s.value?.typePlan?s.value.typePlan.toUpperCase()===l:!1,R=l=>{if(!s.value?.modeControle)return!1;const t=s.value.modeControle.toUpperCase();return l==="RÉDUIT"?t==="REDUIT"||t==="RÉDUIT":l==="RENFORCÉ"?t==="RENFORCE"||t==="RENFORCÉ":t===l};return(l,t)=>(n(),a("div",G,[C(D,{class:"mb-6",titre:"Fiche d'échantillonnage",complement:"ISO 2859","sous-titre":"Plan de contrôle dynamique généré pour l'OF",retour:"",onRetour:V}),_.value?(n(),a("div",H,[C(p(P))])):x.value?(n(),a("div",J,[t[7]||(t[7]=e("i",{class:"pi pi-exclamation-triangle text-2xl"},null,-1)),e("p",null,o(x.value),1)])):s.value?(n(),a("div",K,[e("div",Q,[t[18]||(t[18]=e("div",{class:"bg-[#1f2937] text-white px-5 py-3 flex items-center gap-2 font-bold uppercase tracking-wide text-[13px]"},[e("i",{class:"pi pi-file-o text-green-400"}),d(" Informations Générales ")],-1)),e("div",X,[e("div",null,[e("p",null,[t[8]||(t[8]=e("span",{class:"font-bold text-gray-700"},"Code article :",-1)),d(" "+o(s.value.codeArticle||"-"),1)]),e("p",Y,[t[9]||(t[9]=e("span",{class:"font-bold text-gray-700"},"Atelier :",-1)),d(" "+o(s.value.atelier||"-"),1)]),e("p",Z,[t[10]||(t[10]=e("span",{class:"font-bold text-gray-700 whitespace-nowrap"},"Poste :",-1)),s.value?.estTermine?(n(),a("span",ee,o(s.value.posteCode||"-"),1)):(n(),u(p(E),{key:0,modelValue:s.value.posteCode,"onUpdate:modelValue":t[0]||(t[0]=r=>s.value.posteCode=r),placeholder:"Poste",class:"w-24"},null,8,["modelValue"]))])]),e("div",null,[e("p",null,[t[11]||(t[11]=e("span",{class:"font-bold text-gray-700"},"Désignation :",-1)),t[12]||(t[12]=d()),e("span",te,o(s.value.designation||"-"),1)]),e("p",se,[t[13]||(t[13]=e("span",{class:"font-bold text-gray-700"},"Date de fabrication :",-1)),s.value?.estTermine?(n(),a("span",ne,o(s.value.dateFabrication?s.value.dateFabrication.split("T")[0]:"-"),1)):(n(),a("input",{key:0,type:"date",value:s.value.dateFabrication?s.value.dateFabrication.split("T")[0]:"",onInput:t[1]||(t[1]=r=>s.value.dateFabrication=r.target.value),class:"border border-gray-300 rounded px-2 py-1 text-sm mt-1 w-full focus:outline-none focus:border-blue-500"},null,40,re))]),e("p",oe,[t[14]||(t[14]=e("span",{class:"font-bold text-gray-700"},"Code instrument de mesure :",-1)),e("span",ae,o(s.value.instrumentCodes?.join("  ")||"-"),1)])]),e("div",null,[e("p",null,[t[15]||(t[15]=e("span",{class:"font-bold text-gray-700"},"Numéro de l'OF :",-1)),t[16]||(t[16]=d()),e("span",le,o(s.value.numeroOf||"-"),1)]),e("p",ie,[t[17]||(t[17]=e("span",{class:"font-bold text-gray-700"},"Date de l'échantillonnage :",-1)),s.value?.estTermine?(n(),a("span",pe,o(s.value.dateEchantillonnage?s.value.dateEchantillonnage.substring(0,16).replace("T"," "):"-"),1)):(n(),a("input",{key:0,type:"datetime-local",value:s.value.dateEchantillonnage?s.value.dateEchantillonnage.substring(0,16):"",onInput:t[2]||(t[2]=r=>s.value.dateEchantillonnage=r.target.value),class:"border border-gray-300 rounded px-2 py-1 text-sm mt-1 w-full focus:outline-none focus:border-blue-500"},null,40,de))])])])]),e("div",ue,[t[23]||(t[23]=e("div",{class:"bg-[#1f2937] text-white px-5 py-3 flex items-center gap-2 font-bold uppercase tracking-wide text-[13px]"},[e("i",{class:"pi pi-sliders-h text-green-400"}),d(" Paramètres du plan ")],-1)),e("div",ce,[e("div",null,[t[19]||(t[19]=e("p",{class:"text-xs font-bold text-gray-600 uppercase tracking-wide mb-3"},"Niveau de contrôle",-1)),e("div",ge,[(n(),a(b,null,m(["I","II","III"],r=>e("div",{key:r,class:h(["flex-1 text-center py-2 text-[11px] font-bold rounded transition-colors",I(r)?"bg-[#059669] text-white shadow-sm":"text-gray-400 bg-transparent"])}," NIVEAU "+o(r),3)),64))])]),e("div",null,[t[20]||(t[20]=e("p",{class:"text-xs font-bold text-gray-600 uppercase tracking-wide mb-3"},"Plan d'échantillonnage",-1)),e("div",fe,[(n(),a(b,null,m(["SIMPLE","DOUBLE"],r=>e("div",{key:r,class:h(["flex-1 text-center py-2 text-[11px] font-bold rounded transition-colors",A(r)?"bg-[#059669] text-white shadow-sm":"text-gray-400 bg-transparent"])},o(r),3)),64))])]),e("div",null,[t[21]||(t[21]=e("p",{class:"text-xs font-bold text-gray-600 uppercase tracking-wide mb-3"},"Mode de contrôle",-1)),e("div",xe,[(n(),a(b,null,m(["RÉDUIT","NORMAL","RENFORCÉ"],r=>e("div",{key:r,class:h(["flex-1 text-center py-2 text-[11px] font-bold rounded transition-colors",R(r)?"bg-[#059669] text-white shadow-sm":"text-gray-400 bg-transparent"])},o(r),3)),64))])]),e("div",null,[t[22]||(t[22]=e("p",{class:"text-xs font-bold text-gray-600 uppercase tracking-wide mb-3"},"NQA",-1)),e("div",ye,o(s.value.nqaValeur??"-"),1)])])]),e("div",be,[t[26]||(t[26]=e("div",{class:"bg-[#1f2937] text-white px-5 py-3 flex items-center gap-2 font-bold uppercase tracking-wide text-[13px]"},[e("i",{class:"pi pi-table text-green-400"}),d(" Tableau de détermination de l'échantillon ")],-1)),e("div",me,[e("table",ve,[t[25]||(t[25]=e("thead",null,[e("tr",{class:"bg-[#f8fafc] text-[11px] text-gray-600 font-bold uppercase tracking-wider border-b border-gray-200"},[e("th",{class:"py-4 px-2 border-r border-gray-200 w-16"},"N°"),e("th",{class:"py-4 px-2 border-r border-gray-200"},[d("Effectif du lot"),e("br"),e("span",{class:"text-gray-400 font-normal"})]),e("th",{class:"py-4 px-2 border-r border-gray-200"},[d("Lettre"),e("br"),d("Code")]),e("th",{class:"py-4 px-2 border-r border-gray-200"},"Échantillon global (A)"),e("th",{class:"py-4 px-2 border-r border-gray-200"},"Nb postes (B)"),e("th",{class:"py-4 px-2 border-r border-gray-200"},"Échantillon /poste (A/B)"),e("th",{class:"py-4 px-2 border-r border-gray-200 text-[#059669]"},"Critère d'acceptation"),e("th",{class:"py-4 px-2 text-[#dc2626]"},"Critère de rejet")])],-1)),e("tbody",he,[e("tr",null,[t[24]||(t[24]=e("td",{class:"py-5 px-2 border-r border-gray-200 text-gray-400"},"1",-1)),e("td",ke,o(s.value.tailleLot),1),e("td",we,[s.value?.estTermine?(n(),a("span",_e,o(s.value.lettreCode),1)):(n(),u(p(E),{key:0,modelValue:s.value.lettreCode,"onUpdate:modelValue":t[3]||(t[3]=r=>s.value.lettreCode=r),class:"w-16 text-center"},null,8,["modelValue"]))]),e("td",Ce,[s.value?.estTermine?(n(),a("span",Ee,o(s.value.effectifEchantillonA),1)):(n(),u(p(k),{key:0,modelValue:s.value.effectifEchantillonA,"onUpdate:modelValue":t[4]||(t[4]=r=>s.value.effectifEchantillonA=r),class:"w-20",inputClass:"text-center w-full"},null,8,["modelValue"]))]),e("td",Pe,[s.value?.estTermine?(n(),a("span",Ve,o(s.value.nbPostesB),1)):(n(),u(p(k),{key:0,modelValue:s.value.nbPostesB,"onUpdate:modelValue":t[5]||(t[5]=r=>s.value.nbPostesB=r),class:"w-16",inputClass:"text-center w-full"},null,8,["modelValue"]))]),e("td",Ie,[s.value?.estTermine?(n(),a("span",Ae,o(s.value.effectifParPosteAb),1)):(n(),u(p(k),{key:0,modelValue:s.value.effectifParPosteAb,"onUpdate:modelValue":t[6]||(t[6]=r=>s.value.effectifParPosteAb=r),class:"w-20",inputClass:"text-center w-full"},null,8,["modelValue"]))]),e("td",Re,[e("span",Ne,o(s.value.critereAcceptationAc),1)]),e("td",Te,[e("span",Oe,o(s.value.critereRejetRe),1)])])])])])])])):F("",!0)]))}},qe=O(Se,[["__scopeId","data-v-67c66f8f"]]);export{qe as default};
