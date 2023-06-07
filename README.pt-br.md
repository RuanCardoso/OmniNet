AtomNet - Networking em Nível Subatômico no Unity.
_______________________________________________________________________________

# AtomNet - Um Framework Unity para Desenvolvimento de Jogos Multiplayer

A AtomNet é um framework de Unity criado com o objetivo de proporcionar um alto desempenho no desenvolvimento de jogos multiplayer.

**Requisitos**: O AtomNet requer a versão 2021.2 ou superior do Unity. Isso se deve ao fato de que, a partir da versão 2021.2, o Unity adicionou suporte para o .Net Standard 2.1, trazendo melhorias significativas para o Network Socket, além de outras novidades como Span, Memory, ArrayPool, entre outras.

Decidi aproveitar essas novas funcionalidades para criar um framework de networking de alto desempenho.

## Pré-requisitos

Antes de usar este projeto, certifique-se de ter a seguinte dependência instalada:

- [com.unity.nuget.newtonsoft-json](https://github.com/UnityNuGet/Newtonsoft.Json)

Este pacote é necessário para lidar com a serialização e deserialização de JSON no projeto. No entanto, é importante ressaltar que o Neutron utiliza serialização binária para alcançar alta performance em suas operações.

Você pode instalá-lo através do Unity Package Manager seguindo estes passos:

1. Abra o Unity Editor.
2. Vá para **Window > Package Manager**.
3. Clique no botão **+** no canto superior esquerdo da janela do Package Manager.
4. Selecione **Add package from git URL**.
5. Insira `https://github.com/UnityNuGet/Newtonsoft.Json.git` como a URL do Git.
6. Clique em **Add**.

Assim que o pacote for instalado com sucesso, você poderá começar a usar os recursos de serialização JSON fornecidos pelo Newtonsoft.Json em seu projeto.

No entanto, para obter um desempenho otimizado com o Neutron, ele utiliza serialização binária em vez de JSON para suas operações principais.

## Documentação

[Documentação](../../wiki) - Aqui está a documentação completa do projeto.
