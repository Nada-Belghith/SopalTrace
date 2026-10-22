import{D as c}from"./index-ZHDJ9xz-.js";function f(){const i=c();return{confirmArchivagePlanActif:async({typeDocument:t,identifiant:a,version:e})=>{let n=`Un ${t} actif `;return e!=null&&(n+=`(Version ${e}) `),n+=`existe déjà pour ${a}.

Voulez-vous l'archiver et activer cette nouvelle version ?`,new Promise(r=>{i.require({message:n,header:"Plan Actif Existant",icon:"ri-error-warning-line text-amber-500",acceptLabel:"Oui, archiver",rejectLabel:"Annuler",accept:()=>r(!0),reject:()=>r(!1),onHide:()=>r(!1)})})}}}export{f as u};
