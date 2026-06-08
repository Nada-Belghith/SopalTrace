const data = {
  posteCode: "PAS71",
  nom: "Test API",
  sections: [
    {
      sectionType: "REGLAGE",
      ordreAffiche: 1,
      lignes: [
        { caracteristique: "Test Caract 1", ordreAffiche: 1 }
      ]
    }
  ]
};
fetch('http://localhost:5030/api/planrccf', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(data)
})
.then(r => r.json())
.then(data => console.log(data))
.catch(e => console.error(e));
