const path = require('path');
const prod = process.env.NODE_ENV == 'production';

module.exports = {
    mode: prod ? "production" : "development",
    devtool: 'source-map',
    target: ["web", "es2020"],
    entry: {
        avalonia: './modules/Avalonia/Avalonia.ts',
        avaloniaStorage: {
            import: './modules/Storage/StorageProvider.ts',
            dependOn: 'avalonia',
        }
    },
    output: {
        filename: '[name].js',
        path: path.resolve(__dirname, '../wwwroot'),
        library: {
            type: 'module',
        },
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
            },
        ],
    },
    resolve: {
        extensions: ['.ts', '.js'],
    },
    optimization: {
        minimize: false
    },
    experiments: {
        outputModule: true,
    }
};
