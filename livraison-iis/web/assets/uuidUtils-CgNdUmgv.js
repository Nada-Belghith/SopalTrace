function r(){return typeof crypto<"u"&&crypto.randomUUID?crypto.randomUUID():"id-"+Math.random().toString(36).substring(2,11)+"-"+Date.now().toString(36)}export{r as g};
