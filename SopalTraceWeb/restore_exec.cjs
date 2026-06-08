// eslint-disable-next-line no-undef
const fs = require('fs');
let data = fs.readFileSync('exec_view.json', 'utf8');
if(data.charCodeAt(0) === 0xFEFF) data = data.slice(1);
let lines = data.split('\n');
let jsonLine = lines.find(l => l.includes('"type":"VIEW_FILE"'));
if(jsonLine) {
    let jsonStart = jsonLine.indexOf('{');
    let obj = JSON.parse(jsonLine.substring(jsonStart));
    let content = obj.content;
    let fileLines = content.split('\n');
    let reconstructed = [];
    let started = false;
    for(let l of fileLines) {
        let match = l.match(/^\d+:\s(.*)/);
        if(match) {
            started = true;
            reconstructed.push(match[1]);
        } else if (started && l.trim() === '') {
            reconstructed.push('');
        }
    }
    fs.writeFileSync('src/components/Execution/ExecEncfForm.vue', reconstructed.join('\n'), 'utf8');
    console.log('Restored successfully, lines:', reconstructed.length);
} else {
    console.log('Not found');
}
