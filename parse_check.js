const acorn = require('acorn');
const fs = require('fs');

const code = fs.readFileSync('frontend/interface/app.js', 'utf-8');

try {
    const ast = acorn.parse(code, {
        ecmaVersion: 2022,
        locations: true,
        allowReturnOutsideFunction: true,
        allowAwaitOutsideFunction: true
    });
    console.log('Parse successful!');
} catch (e) {
    console.log('Parse error:', e.message);
    console.log('Location:', e.loc);
    console.log('Position:', e.pos);
    // Show context around error
    const start = Math.max(0, e.pos - 100);
    const end = Math.min(code.length, e.pos + 100);
    console.log('Context:', JSON.stringify(code.slice(start, end)));
}